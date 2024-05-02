using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace DBImportExport
{
    class TableScriptInfo
    {
        #region private members
        private string m_sFilter;
        private string m_sName;
        private string m_sSchema;
        private enumScriptMode m_ScriptMode;
        private string m_sFilePath;
        #endregion private members

        #region Constructors and Destructors

        public TableScriptInfo()
        {
            Schema = "dbo";
        }

        public TableScriptInfo(FileInfo CurFile)
        {
            m_sFilePath = CurFile.FullName;
            string[] sArr = CurFile.Name.Split(new char[] { '.' }); // , '[', ']'
            Schema = Regex.Replace((sArr.GetLength(0) > 3) ? sArr[1] : sArr[0], @"(\[)|([\.\]])|(.*?\.)|(\.)", "", RegexOptions.IgnoreCase); 
            Name = Regex.Replace((sArr.GetLength(0) > 3) ? sArr[2] : sArr[1], @"(\[)|([\.\]])|(.*?\.)|(\.)", "", RegexOptions.IgnoreCase); 
        }
        public TableScriptInfo(string tableArgs)
        {
            Schema = "dbo";
            tableArgs = tableArgs.Trim();
            if (tableArgs.ToLowerInvariant().EndsWith(" with nodelete"))
            {
                int z = tableArgs.ToLowerInvariant().LastIndexOf(" with nodelete");
                tableArgs = tableArgs.Substring(0, z).Trim();
                ScriptMode = enumScriptMode.NoDelete;
            }
            else if (tableArgs.ToLowerInvariant().EndsWith(" with delete"))
            {
                int z = tableArgs.ToLowerInvariant().LastIndexOf(" with delete");
                tableArgs = tableArgs.Substring(0, z).Trim();
                ScriptMode = enumScriptMode.Delete;
            }
            else
            {
                ScriptMode = enumScriptMode.NotSet;
            }

            int i = tableArgs.ToLower().IndexOf(" where ");
            string sTmp = tableArgs;
            if (i > -1)
            {
                sTmp = tableArgs.Substring(0, i);
                Filter = tableArgs.Substring(i + 1, tableArgs.Length - (i + 1)).Trim();
            }

            i = sTmp.ToLower().IndexOf(".");
            if (i > -1)
            {
                Schema = Regex.Replace(sTmp.Substring(0, i), @"(\[)|([\.\]])|(.*?\.)|(\.)", "", RegexOptions.IgnoreCase);
            }
            
            Name = Regex.Replace(sTmp, @"(\[)|([\.\]])|(.*?\.)|(\.)", "", RegexOptions.IgnoreCase);
        }

        public TableScriptInfo(string tableName, string filter, enumScriptMode scriptMode)
        {
            Schema = "dbo";
            Name = Regex.Replace(tableName, @"(\[)|([\.\]])|(.*?\.)|(\.)", "", RegexOptions.IgnoreCase);
            Filter = filter;
            ScriptMode = scriptMode;
        }
        public TableScriptInfo(string tableName, string filter, enumScriptMode scriptMode, string aSchema)
        {
            Schema = aSchema;
            Name = Regex.Replace(tableName, @"(\[)|([\.\]])|(.*?\.)|(\.)", "", RegexOptions.IgnoreCase);
            Filter = filter;
            ScriptMode = scriptMode;
        }

        #endregion

        #region Properties

        public string Filter
        {
            get
            {
                if (!string.IsNullOrEmpty(m_sFilter))
                {
                    m_sFilter = Regex.Replace(m_sFilter.Trim(), @"^where\s", "", RegexOptions.IgnoreCase).Trim();
                    return m_sFilter;
                }
                return string.Empty;
            }
            set
            {
                m_sFilter = value;
            }
        }

        public string Name
        {
            get
            {
                return m_sName;
            }
            set
            {
                m_sName = value;
            }
        }

        public string Schema
        {
            get
            {
                return m_sSchema;
            }
            set
            {
                m_sSchema = value;
            }
        }

        public string QualifiedName
        {
            get
            {
                return string.Format("[{0}].[{1}]", Schema, Name);
            }
        }

        public string FilePath
        {
            get
            {
                return m_sFilePath;
            }
            set
            {
                m_sFilePath = value; 
            }
        }

        public enumScriptMode ScriptMode
        {
            get
            {
                return m_ScriptMode;
            }
            set
            {
                m_ScriptMode = value;
            }
        }

        #endregion

        #region Public Methods

        #endregion
    }
}
