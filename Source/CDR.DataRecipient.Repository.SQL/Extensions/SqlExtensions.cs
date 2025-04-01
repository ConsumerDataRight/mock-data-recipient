using Microsoft.Data.SqlClient;
using System;

namespace CDR.DataRecipient.Repository.SQL.Extensions
{
    public static class SqLExtensions
    {
        /// <summary>
        /// Execute scalar command and return result as Int32. Throw error if no results or conversion error.
        /// </summary>
        public static int ExecuteScalarInt32(this SqlCommand command)
        {
            var res = command.ExecuteScalar();

            if (res == DBNull.Value || res == null)
            {
                throw new System.Data.DataException("Command returns no results");
            }

            return Convert.ToInt32(res);
        }

        /// <summary>
        /// Execute scalar command and return result as string. Throw error if no results or conversion error.
        /// </summary>
        public static string ExecuteScalarString(this SqlCommand command)
        {
            var res = command.ExecuteScalar();

            if (res == DBNull.Value || res == null)
            {
                throw new System.Data.DataException("Command returns no results");
            }

            return Convert.ToString(res);
        }
    }
}
