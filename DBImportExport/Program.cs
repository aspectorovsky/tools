using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace DBImportExport
{
    public enum enumMode
    {
        Import,
        Export
    }
    public enum enumScriptMode
    {
        NotSet, 
        Delete, 
        NoDelete
    }
    class Program
    {
        static private TextWriterTraceListener m_textListener;
        static private enumMode m_Mode;
        static private TraceLevel m_VerboseLevel = TraceLevel.Error;
        static private string m_sConnectionString, m_sScriptDirectory, m_sBCP_Options;
        static private string m_sUser, m_sPassword;
        static private string m_sSQLServerInst, m_sDatabase;
        static private string m_sExportFilter4Tables;
        static private bool m_IsBCP = false;
        static private bool m_Is_SP_Script = false;
        static private bool m_Is_Table_Script = false;
        static private enumScriptMode m_defaultScriptMode = enumScriptMode.NoDelete;
        static private List<TableScriptInfo> m_lstTableScriptInfo = new List<TableScriptInfo>();
        static private Database m_Database;
        

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                HelpInfoPrint();
                return;
            }
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Tracer.Trace.Level = m_VerboseLevel;
            try
            {
                if (!GetParamFromConsole(args))
                    return;

                if (m_Mode == enumMode.Export)
                    Export();
                else if (m_Mode == enumMode.Import)
                    Import();
            }
            catch (System.Exception ce)
            {
                Trace.WriteLineIf(Tracer.Trace.TraceError, ce.Message);
            }
            finally
            {
                Trace.Flush();
                if (m_textListener != null)
                {
                    m_textListener.Flush();
                    m_textListener.Close();
                }
            }
        }

        private static void Export()
        {
            if (m_lstTableScriptInfo.Count == 1)
            {
                TableScriptInfo CurTableScriptInfo = m_lstTableScriptInfo[0];
                if (CurTableScriptInfo.Name == "*")
                {
                    Trace.WriteLineIf(Tracer.Trace.TraceInfo, "Get name of  tables");
                    Trace.WriteLineIf(Tracer.Trace.TraceInfo, "m_sExportFilter4Tables : " + m_sExportFilter4Tables);
                    Trace.WriteLineIf(Tracer.Trace.TraceInfo, "SQL 4 Get Tables : " + "Select Object_Name(a.object_id) as TableName, b.name as SchemaName From sys.objects a inner join sys.schemas b on a.schema_id = b.schema_id Where type='U'" + (string.IsNullOrEmpty(m_sExportFilter4Tables) ? string.Empty : " AND ( " + m_sExportFilter4Tables + " )"));
                    using (
                        IDbCommand cmd =
                            m_Database.CreateCommand(
                            "Select Object_Name(a.object_id) as TableName, b.name as SchemaName From sys.objects a inner join sys.schemas b on a.schema_id = b.schema_id Where type='U'" + (string.IsNullOrEmpty(m_sExportFilter4Tables) ? string.Empty : " AND ( " + m_sExportFilter4Tables + " )")))
                    {
                        cmd.CommandType = CommandType.Text;
                        using (IDataReader reader = m_Database.ExecuteReader(cmd))
                        {
                            m_lstTableScriptInfo = new List<TableScriptInfo>();
                            while (reader.Read())
                            {
                                m_lstTableScriptInfo.Add(
                                    new TableScriptInfo((string)reader["TableName"], string.Empty, CurTableScriptInfo.ScriptMode, (string)reader["SchemaName"]));
                            }
                        }
                        Trace.WriteLineIf(Tracer.Trace.TraceInfo, "Count of  tables :" + m_lstTableScriptInfo.Count.ToString());
                    }
                }
            }
            DirectoryInfo dirDirData = new DirectoryInfo(m_sScriptDirectory + "\\Data");
            if (dirDirData.Exists == false)
                dirDirData.Create();

            if (m_Is_SP_Script)
            {
                    Trace.WriteLineIf(Tracer.Trace.TraceInfo, "Get sql scripts for StoreProcedure, Functions, Triggers, Views");
                    using (
                        IDbCommand cmd =
                            m_Database.CreateCommand(
                                @"Select db_name() + '.' + b.name + '.' + a.name as ObjName, type_desc as ObjType 
From sys.objects a inner join sys.schemas b on a.schema_id = b.schema_id
WHERE type_desc in ('SQL_SCALAR_FUNCTION', 'SQL_STORED_PROCEDURE', 'SQL_TABLE_VALUED_FUNCTION', 'SQL_TRIGGER','VIEW')"))
                    {
                        cmd.CommandType = CommandType.Text;
                        using (IDataReader reader = m_Database.ExecuteReader(cmd))
                        {
                            Dictionary<string, DirectoryInfo> dictDirs = new Dictionary<string, DirectoryInfo>();
                            DirectoryInfo dirDir4SQL;
                            while (reader.Read())
                            {
                                string sObjName, sObjType;
                                sObjName = (string)reader["ObjName"];
                                sObjType = (string)reader["ObjType"];
                                Trace.WriteLineIf(Tracer.Trace.TraceInfo, "Start write sql for " + sObjName + " (" + sObjType + ")");
                                if (dictDirs.ContainsKey(sObjType) == false)
                                {
                                    dirDir4SQL = new DirectoryInfo(m_sScriptDirectory + "\\" + sObjType);
                                    if (dirDir4SQL.Exists == false)
                                        dirDir4SQL.Create();
                                    dictDirs.Add(sObjType, dirDir4SQL);
                                }
                                else
                                    dirDir4SQL = dictDirs[sObjType];
                                //FileInfo fi = new FileInfo(dirDir4SQL.FullName + "\\" + sObjName + ".sql");
                                //if (fi.Exists)
                                //    fi.Delete();
                                StreamWriter swSQL = new StreamWriter(dirDir4SQL.FullName + "\\" + sObjName + ".sql", false);
                                using (IDbCommand cmdSQL = m_Database.CreateCommand(@"exec sp_helptext '" + sObjName + "'"))
                                {
                                    cmdSQL.CommandType = CommandType.Text;
                                    using (IDataReader readerSQL = m_Database.ExecuteReader(cmdSQL))
                                    {
                                        while (readerSQL.Read())
                                        {
                                            if (readerSQL["Text"].ToString().Length > 1)
                                            {
                                                swSQL.Write(readerSQL["Text"].ToString());
                                            }
                                        }
                                    }
                                }
                                swSQL.Close();
                            }
                        }
                    }
            }

            DirectoryInfo dirDir4TableSQL = null;
            if (m_Is_Table_Script)
            {
                dirDir4TableSQL = new DirectoryInfo(m_sScriptDirectory + "\\Table");
                if (dirDir4TableSQL.Exists == false)
                    dirDir4TableSQL.Create();
            }


            foreach (TableScriptInfo CurTableScriptInfo in m_lstTableScriptInfo)
            {
                if (m_Is_Table_Script)
                {
                    Trace.WriteLineIf(Tracer.Trace.TraceInfo, "Start write sql for " + CurTableScriptInfo.QualifiedName + " (" + "TABLE" + ")");
                    using (
                        IDbCommand cmd =
                            m_Database.CreateCommand(
                                string.Format(@"
CREATE TABLE #sqlw_pkeys (  t_q SYSNAME, t_o SYSNAME, t_n SYSNAME, cn SYSNAME, ks INT, pn SYSNAME  )
INSERT INTO #sqlw_pkeys EXEC sp_pkeys @table_name = N'{1}',@table_owner = N'{2}';
SELECT  [COLNAME] = i_s.column_name,  
[DATATYPE] = UPPER(DATA_TYPE)  
+ CASE WHEN DATA_TYPE IN ('NUMERIC', 'DECIMAL') THEN  
'(' + CAST(NUMERIC_PRECISION AS VARCHAR)  
+ ', ' + CAST(NUMERIC_SCALE AS VARCHAR) + ')'  
ELSE '' END  
+ CASE COLUMNPROPERTY(OBJECT_ID('{0}'), COLUMN_NAME, 'IsIdentity')  
WHEN 1 THEN  
' IDENTITY (' + CAST(IDENT_SEED('{0}') AS VARCHAR(32)) + ', ' + CAST(IDENT_INCR('{0}') AS VARCHAR(32)) + ')' ELSE '' END  
+ CASE RIGHT(DATA_TYPE, 4) WHEN 'CHAR' THEN  
' ('+CASE CHARACTER_MAXIMUM_LENGTH WHEN -1 THEN 'max' ELSE CAST(CHARACTER_MAXIMUM_LENGTH AS VARCHAR) END +')' ELSE '' END  
+ CASE IS_NULLABLE WHEN 'No' THEN ' NOT ' ELSE ' ' END  
+ 'NULL' + COALESCE(' DEFAULT ' + SUBSTRING(COLUMN_DEFAULT,  
2, LEN(COLUMN_DEFAULT)-2), ''),  
[PMKey] = CASE WHEN pk.cn IS NOT NULL THEN 'Yes' ELSE '' END 
FROM  INFORMATION_SCHEMA.COLUMNS i_s  LEFT OUTER JOIN  
#sqlw_pkeys pk  
ON pk.cn = i_s.column_name  LEFT OUTER JOIN 
sys.extended_properties  s 
ON s.major_id = OBJECT_ID(i_s.TABLE_SCHEMA+'.'+i_s.TABLE_NAME)  
AND s.minor_id = i_s.ORDINAL_POSITION  
AND s.name = 'MS_Description'  
WHERE  
i_s.TABLE_NAME = '{1}' AND i_s.TABLE_SCHEMA = '{2}' ORDER BY 
i_s.ORDINAL_POSITION", CurTableScriptInfo.QualifiedName,CurTableScriptInfo.Name , CurTableScriptInfo.Schema)))
                    {
                        cmd.CommandType = CommandType.Text;
                        using (IDataReader reader = m_Database.ExecuteReader(cmd))
                        {
                            string strCreateTable = "Create Table " + CurTableScriptInfo.QualifiedName + " (";
                            string primaryKey = "";
                            while (reader.Read())
                            {
                                strCreateTable += "\r\n[" + reader["ColName"].ToString() + "] " + reader["DataType"].ToString() + ",";
                                if (reader["PMKey"].ToString() == "Yes")
                                    primaryKey += ",[" + reader["ColName"].ToString() + "]";
                            }
                            strCreateTable = strCreateTable.Substring(0, strCreateTable.Length - 1);
                            if (primaryKey != "")
                            {
                                primaryKey = primaryKey.Substring(1, primaryKey.Length - 1);
                                strCreateTable += ", Primary Key (" + primaryKey + ")";
                            }
                            strCreateTable += ")\r\n";
                            StreamWriter swSQL = new StreamWriter(dirDir4TableSQL.FullName + "\\" + CurTableScriptInfo.QualifiedName + ".sql", false);
                            swSQL.WriteLine(strCreateTable);
                            swSQL.Close();
                        }
                    }
                }
                if (CurTableScriptInfo.ScriptMode == enumScriptMode.NotSet)
                {
                    CurTableScriptInfo.ScriptMode = m_defaultScriptMode;
                }
                if (m_IsBCP)
                {
                    string sLogin = "-T";
                    if (string.IsNullOrEmpty(m_sUser) == false && string.IsNullOrEmpty(m_sPassword) == false)
                        sLogin = string.Format("-U {0} -P {1}", m_sUser, m_sPassword);
                    Trace.WriteLineIf(Tracer.Trace.TraceInfo, "Begin export table " + CurTableScriptInfo.QualifiedName);
                    Process p = new Process();
                    p.StartInfo.FileName = "bcp.exe";
                    if (string.IsNullOrEmpty( CurTableScriptInfo.Filter))
                        p.StartInfo.Arguments = string.Format("{0}.{1} out {2}\\{1}.csv -e {2}\\{1}_exp.err {3} {5} -S {4} -c", m_sDatabase, CurTableScriptInfo.QualifiedName, dirDirData.FullName, m_sBCP_Options, m_sSQLServerInst, sLogin);
                    else
                        p.StartInfo.Arguments = string.Format("\"Select * From {0}.{1} WHERE {2} \" queryout {3}\\{1}.csv -e {3}\\{1}.err {4} {6} -S {5} -c", m_sDatabase, CurTableScriptInfo.QualifiedName, CurTableScriptInfo.Filter, dirDirData.FullName, m_sBCP_Options, m_sSQLServerInst, sLogin);
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.Start();

                    string output = p.StandardOutput.ReadToEnd();

                    Trace.WriteLineIf(Tracer.Trace.TraceVerbose, output);
                    FileInfo fi = new FileInfo(string.Format("{0}\\{1}_exp.err", dirDirData.FullName, CurTableScriptInfo.QualifiedName));
                    if(fi.Exists && fi.Length == 0)
                        fi.Delete();
                }
            }
        }

        private static void CreateSchema(string sSchemaName)
        {
            try
            {
                using (
                    IDbCommand cmd =
                        m_Database.CreateCommand(
                            string.Format(@"CREATE SCHEMA [{0}] AUTHORIZATION [db_owner] ", sSchemaName)))
                {
                    cmd.CommandType = CommandType.Text;
                    int ret = m_Database.ExecuteNonQuery(cmd);
                }
            }
            catch (System.Exception ce)
            {
                Trace.WriteLineIf(Tracer.Trace.TraceError, ce.Message);
            }
        }

        private static void DeleteObject(string sObjType, string sObjName)
        {
            try
            {
                if (sObjType == "TABLE" && m_Is_SP_Script)
                {
                    using (
                        IDbCommand cmd =
                            m_Database.CreateCommand(
                                string.Format(@"IF OBJECT_ID('{1}') is not null 
    BEGIN
    DECLARE @ObjType VARCHAR(256)
    DECLARE @ObjName VARCHAR(256)
    DECLARE @cmd VARCHAR(max)

    if Object_id('TempDB..#Table_depends') is NULL
    CREATE TABLE #Table_depends
    (name varchar(256),
    ObjType varchar(50)
    )
    else
    delete from #Table_depends

    DECLARE db_cursor CURSOR FOR
    SELECT * from #Table_depends

    insert into #Table_depends
    (name,ObjType) Exec sp_depends @objname = '{1}'

    OPEN db_cursor
    FETCH NEXT FROM db_cursor INTO @ObjName, @ObjType

    WHILE @@FETCH_STATUS = 0
    BEGIN
    IF @ObjType = 'view' OR @ObjType = 'trigger'  
    BEGIN
	    SET @cmd = 'DROP ' + @ObjType + ' ' + @ObjName
        EXEC (@cmd)
    END
    FETCH NEXT FROM db_cursor INTO @ObjName, @ObjType
    END

    CLOSE db_cursor
    DEALLOCATE db_cursor
    DROP {0} {1}
    END", sObjType, sObjName)))
                    {
                        cmd.CommandType = CommandType.Text;
                        int ret = m_Database.ExecuteNonQuery(cmd);
                    }
                }
                else
                {
                    using (
                        IDbCommand cmd =
                            m_Database.CreateCommand(
                                string.Format(@"IF OBJECT_ID('{1}') is not null DROP {0} {1}", sObjType, sObjName)))
                    {
                        cmd.CommandType = CommandType.Text;
                        int ret = m_Database.ExecuteNonQuery(cmd);
                    }
                }
            }
            catch (System.Exception ce)
            {
                Trace.WriteLineIf(Tracer.Trace.TraceError, ce.Message);
            }
        }

        private static void Import(Dictionary<string, string> dictSchemas, DirectoryInfo CurDir)
        {
            string sObjType = string.Empty; 
            switch (CurDir.Name.ToUpper())
            {
                case "SQL_SCALAR_FUNCTION":
                case "SQL_TABLE_VALUED_FUNCTION":
                    sObjType = "FUNCTION";
                    if (!m_Is_SP_Script)
                        return;
                    break;
                case "SQL_STORED_PROCEDURE":
                    sObjType = "PROCEDURE";
                    if (!m_Is_SP_Script)
                        return;
                    break;
                case "SQL_TRIGGER":
                    sObjType = "TRIGGER";
                    if (!m_Is_SP_Script)
                        return;
                    break;
                case "VIEW":
                    sObjType = "VIEW";
                    if (!m_Is_SP_Script)
                        return;
                    break;
                case "TABLE":
                    sObjType = "TABLE";
                    if (!m_Is_Table_Script)
                        return;
                    break;
            }
            FileInfo[] files = CurDir.GetFiles("*.sql");
            foreach (FileInfo CurFile in files)
            {
                try
                {
                    Trace.WriteLineIf(Tracer.Trace.TraceInfo, "Start import for " + CurFile.Name);
                    string sSchema;
                    string[] sArr = CurFile.Name.Split(new char[] { '.' }); // , '[', ']'
                    sSchema = (sArr.GetLength(0) > 3) ? sArr[1] : sArr[0];
                    sSchema = sSchema.Trim(new char[] { '[', ']' });
                    if (dictSchemas.ContainsKey(sSchema) == false)
                    {
                        CreateSchema(sSchema);
                        dictSchemas.Add(sSchema, sSchema);
                    }
                    using (StreamReader sr = new StreamReader(CurFile.FullName))
                    {
                        string sCommand = sr.ReadToEnd();
                        if (string.IsNullOrEmpty(sObjType) == false)
                        {
                            if (sArr.GetLength(0)>3)
                                DeleteObject(sObjType, string.Join(".", sArr, 1, 2));
                            else
                                DeleteObject(sObjType, string.Join(".", sArr, 0, 2));
                        }
                        using (
                            IDbCommand cmd =
                                m_Database.CreateCommand(sCommand))
                        {
                            cmd.CommandType = CommandType.Text;
                            int ret = m_Database.ExecuteNonQuery(cmd);
                        }
                    }
                }
                catch (System.Exception ce)
                {
                    Trace.WriteLineIf(Tracer.Trace.TraceError, ce.Message);
                }
                finally
                {
                    Trace.WriteLineIf(Tracer.Trace.TraceInfo, "End import for " + CurFile.Name);
                }
            }
        }

        private static void Import()
        {
            Trace.WriteLineIf(Tracer.Trace.TraceInfo, "Start Import");
            DirectoryInfo dirInputDir = new DirectoryInfo(m_sScriptDirectory);
            if (dirInputDir.Exists == false)
                new ArgumentException("Input directory " + m_sScriptDirectory + "is not exist", "ScriptDirectory");
            Dictionary<string, string> dictSchemas = new Dictionary<string,string>();

            DirectoryInfo[] arrDirs = dirInputDir.GetDirectories();
            if (m_Is_Table_Script)
            {
                DirectoryInfo CurDir = new DirectoryInfo(m_sScriptDirectory + "\\Table"  );
                if(CurDir.Exists == false)
                    new ArgumentException("Input directory " + CurDir.FullName + "is not exist", "Is_Table_Script");

                Trace.WriteLineIf(Tracer.Trace.TraceInfo, "Start process for " + CurDir.Name);
                Import(dictSchemas, CurDir);
                Trace.WriteLineIf(Tracer.Trace.TraceInfo, "End process for " + CurDir.Name);
            }
            if (m_Is_SP_Script)
            {
                foreach (DirectoryInfo CurDir in arrDirs)
                {
                    if (CurDir.Name == "Data" || CurDir.Name == "Table")
                        continue;
                    Trace.WriteLineIf(Tracer.Trace.TraceInfo, "Start process for " + CurDir.Name);
                    Import(dictSchemas, CurDir);
                    Trace.WriteLineIf(Tracer.Trace.TraceInfo, "End process for " + CurDir.Name);
                }
            }

            DirectoryInfo CurDirData = new DirectoryInfo(m_sScriptDirectory + "\\Data");
            if (CurDirData.Exists == false)
                new ArgumentException("Input directory " + CurDirData.FullName + "is not exist");
            if (m_lstTableScriptInfo.Count == 1)
            {
                TableScriptInfo CurTableScriptInfo = m_lstTableScriptInfo[0];
                if (CurTableScriptInfo.Name == "*")
                {
                    Trace.WriteLineIf(Tracer.Trace.TraceInfo, "Get name of tables");
                    m_lstTableScriptInfo = new List<TableScriptInfo>();
                    FileInfo[] files = CurDirData.GetFiles("*.csv");
                    foreach (FileInfo CurFile in files)
                    {
                        m_lstTableScriptInfo.Add(new TableScriptInfo(CurFile));
                    }
                }
            }

            foreach (TableScriptInfo CurTableScriptInfo in m_lstTableScriptInfo)
            {
                if (CurTableScriptInfo.ScriptMode == enumScriptMode.NotSet)
                    CurTableScriptInfo.ScriptMode = m_defaultScriptMode;

                if (string.IsNullOrEmpty(CurTableScriptInfo.FilePath))
                {
                    FileInfo CurFileInfo = new FileInfo(CurDirData.FullName + "\\" + CurTableScriptInfo.QualifiedName + ".csv");
                    if (CurFileInfo.Exists == false)
                        continue;
                    CurTableScriptInfo.FilePath = CurFileInfo.FullName;
                }
                Trace.WriteLineIf(Tracer.Trace.TraceInfo, "Begin import table " + CurTableScriptInfo.QualifiedName);
                if (CurTableScriptInfo.ScriptMode == enumScriptMode.Delete)
                {
                    string queryString;
                    if (string.IsNullOrEmpty(CurTableScriptInfo.Filter))
                    {
                        queryString = string.Format("DELETE FROM {0}", CurTableScriptInfo.QualifiedName);
                    }
                    else
                    {
                        queryString = string.Format(
                            "DELETE FROM {0} WHERE {1}", CurTableScriptInfo.QualifiedName, CurTableScriptInfo.Filter);
                    }
                    try
                    {
                        using ( IDbCommand cmd = m_Database.CreateCommand(queryString) )
                        {
                            cmd.CommandType = CommandType.Text;
                            Trace.WriteLineIf(Tracer.Trace.TraceInfo, @"Starting delete...");
                            int ret = m_Database.ExecuteNonQuery(cmd);
                            Trace.WriteLineIf(Tracer.Trace.TraceInfo, string.Format(@"{0} rows deleted.", ret ));
                        }
                    }
                    catch (System.Exception ce)
                    {
                        Trace.WriteLineIf(Tracer.Trace.TraceError, ce.Message);
                    }
                }
                if (m_IsBCP)
                {
                    string sLogin = "-T";
                    if (string.IsNullOrEmpty(m_sUser) == false && string.IsNullOrEmpty(m_sPassword) == false)
                        sLogin = string.Format("-U {0} -P {1}", m_sUser, m_sPassword);
                    Process p = new Process();
                    p.StartInfo.FileName = "bcp.exe";
                    p.StartInfo.Arguments = string.Format("{0}.{1} in {2} -e {6}\\{1}_imp.err {3} {5} -S {4} -c", m_sDatabase, CurTableScriptInfo.QualifiedName, CurTableScriptInfo.FilePath, m_sBCP_Options, m_sSQLServerInst, sLogin, CurDirData.FullName);
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.Start();

                    string output = p.StandardOutput.ReadToEnd();

                    Trace.WriteLineIf(Tracer.Trace.TraceVerbose, output);
                    FileInfo fi = new FileInfo(string.Format("{0}\\{1}_imp.err", CurDirData.FullName, CurTableScriptInfo.QualifiedName));
                    if (fi.Exists && fi.Length == 0)
                        fi.Delete();
                }
                Trace.WriteLineIf(Tracer.Trace.TraceInfo, "End import table " + CurTableScriptInfo.QualifiedName);
            }

            Trace.WriteLineIf(Tracer.Trace.TraceInfo, "End Import");
        }

        private static bool GetParamFromConsole(string[] args)
        {
            try
            {
                m_Mode = (enumMode)Enum.Parse(typeof(enumMode), args[0].Substring(1), true);
                foreach (string s in args)
                {
                    string[] sArr = s.Split(new char[] { ':' });
                    switch (sArr[0].ToLowerInvariant())
                    {
                        case "-connectionstring":
                            m_sConnectionString = sArr[1];
                            m_Database = new Database(m_sConnectionString);
                            {
                                string[] sArrConn = m_sConnectionString.Split(new char[] { ';' });
                                foreach(string sParam in sArrConn)
                                {
                                    string[] sArrConnParam = sParam.Split(new char[] { '=' });
                                    switch(sArrConnParam[0].Trim().ToLowerInvariant())
                                    {
                                        case "data source":
                                            m_sSQLServerInst = sArrConnParam[1];
                                            break;
                                        case "initial catalog":
                                            m_sDatabase = sArrConnParam[1];
                                            break;
                                        case "uid":
                                        case "user id":
                                            m_sUser = sArrConnParam[1];
                                            break;
                                        case "pwd":
                                        case "password":
                                            m_sPassword = sArrConnParam[1];
                                            break;
                                    }
                                }
                            }
                            break;
                        case "-bcp":
                            m_IsBCP = true;
                            break;
                        case "-sp_script":
                            m_Is_SP_Script = true;
                            break;
                        case "-table_script":
                            m_Is_Table_Script = true;
                            break;
                        case "-tables":
                            for(int i = 1; i < sArr.Count(); i++ )
                            {
                                m_lstTableScriptInfo.Add(new TableScriptInfo(sArr[i]));
                            }
                            break;
                        case "-defaultscriptmode":
                            m_defaultScriptMode = (enumScriptMode)Enum.Parse(typeof(enumScriptMode), sArr[1], true);
                            break;
                        case "-scriptdirectory":
                            m_sScriptDirectory = string.Join(":", sArr, 1, sArr.Count()-1);
                            DirectoryInfo dir = new DirectoryInfo(m_sScriptDirectory);
                            if (dir.Exists == false)
                            {
                                dir.Create();
                                m_sScriptDirectory = dir.FullName;
                            }
                                FileInfo fi = new FileInfo(dir.FullName + "\\DBImportExport.log");
                                if (fi.Exists)
                                fi.Delete();
                            m_textListener = new TextWriterTraceListener(fi.FullName);
                            Trace.Listeners.Add(m_textListener);
                            break;
                        case "-verbose":
                            m_VerboseLevel = (TraceLevel)Enum.Parse(typeof(TraceLevel), sArr[1], true);
                            Tracer.Trace.Level = m_VerboseLevel;
                            break;
                        case "-bcp_options":
                            m_sBCP_Options = sArr[1];
                            break;
                        case "-exportfilter4tables":
                            m_sExportFilter4Tables = sArr[1];
                            break;
                    }
                }
                if(m_lstTableScriptInfo.Count() < 1)
                {
                    throw new ArgumentException("Tables data is undefined");
                }
            }
            catch (System.Exception ce)
            {
                Trace.WriteLineIf(Tracer.Trace.TraceError, ce.Message);
                return false;
            }
            return true;
        }

        private static void HelpInfoPrint()
        {
            //Console.Clear();
            Console.WriteLine(Properties.Resources.HelpMessage.Replace("%0%", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()));
        }
    }
}
