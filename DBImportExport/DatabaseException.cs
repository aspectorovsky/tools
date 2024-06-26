﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBImportExport
{
    public class DatabaseException : ApplicationException
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public DatabaseException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public DatabaseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseException"/> class.
        /// </summary>
        /// <param name="formatString">The format string.</param>
        /// <param name="args">The args.</param>
        public DatabaseException(string formatString, params string[] args)
            : base(string.Format(formatString, args))
        {
        }

        #endregion
    }
}
