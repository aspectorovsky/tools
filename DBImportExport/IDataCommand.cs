﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace DBImportExport
{
    public interface IDataCommand : IDisposable
    {
        #region Properties

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        /// <value>The connection string.</value>
        string ConnectionString { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds the parameter with value to the given command. This is becuase IDbCommand
        /// does not include the AddWithValue method.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        void AddParameterWithValue(IDbCommand command, string name, object value);

        /// <summary>
        /// Begins a new active transaction.
        /// </summary>
        void BeginTransaction();

        /// <summary>
        /// Commits the active transaction.
        /// </summary>
        void Commit();

        /// <summary>
        /// Creates the concrete command object to use with this instance.
        /// </summary>
        /// <param name="commandText">The command text.</param>
        /// <returns></returns>
        IDbCommand CreateCommand(string commandText);

        /// <summary>
        /// Executes the non query.
        /// </summary>
        /// <param name="cmd">The CMD.</param>
        /// <returns></returns>
        int ExecuteNonQuery(IDbCommand cmd);

        /// <summary>
        /// Executes the reader.
        /// </summary>
        /// <param name="cmd">The CMD.</param>
        /// <returns></returns>
        IDataReader ExecuteReader(IDbCommand cmd);

        /// <summary>
        /// Executes the scalar.
        /// </summary>
        /// <param name="cmd">The CMD.</param>
        /// <returns></returns>
        object ExecuteScalar(IDbCommand cmd);

        /// <summary>
        /// Rolls back the active transaction.
        /// </summary>
        void Rollback();

        #endregion
    }
}
