using System;

namespace OpenTDB.Exceptions
{
    /// <summary>
    /// Exception thrown for errors specific to the OpenTDB library.
    /// </summary>
    public class OpenTDBException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OpenTDBException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public OpenTDBException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenTDBException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception, or a <c>null</c> reference if no inner exception is specified.
        /// </param>
        public OpenTDBException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}