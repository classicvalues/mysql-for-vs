// Copyright (C) 2004-2006 MySQL AB
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License version 2 as published by
// the Free Software Foundation
//
// There are special exceptions to the terms and conditions of the GPL 
// as it is applied to this software. View the full text of the 
// exception in file EXCEPTIONS in the directory of this software 
// distribution.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using System;
using NUnit.Framework;
using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;

namespace MySql.Data.MySqlClient.Tests
{
	/// <summary>
	/// Summary description for BaseTest.
	/// </summary>
	public class BaseTest
	{
		protected MySqlConnection conn;
		protected string table;
		protected string csAdditions = String.Empty;
		protected string host;
		protected string user;
		protected string password;
        protected int port;
        protected string database;

		public BaseTest()
		{
            csAdditions = ";pooling=false;";
            user = "root";
            password = "";
            host = "localhost";
            database = "test";
            port = 3306;

            object o = ConfigurationSettings.AppSettings["port"];
            if (o != null)
                port = Int32.Parse((string)o);
            o = ConfigurationSettings.AppSettings["database"];
            if (o != null)
                database = (string)o;
            o = ConfigurationSettings.AppSettings["userid"];
            if (o != null)
                user = (string)o;
            o = ConfigurationSettings.AppSettings["password"];
            if (o != null)
                password = (string)o;
        }

		protected virtual string GetConnectionInfo()
		{
            return String.Format("protocol=tcp;port={0}", port);
		}

		protected string GetConnectionString(bool includedb)
		{
			string connStr = String.Format("server={0};user id={1};password={2};" +
				 "persist security info=true;{3}", host, user, password, csAdditions);
			if (includedb)
                connStr += String.Format("database={0};", database);
			connStr += GetConnectionInfo();
			return connStr;
		}

		protected void Open()
		{
			try
			{
				string connString = GetConnectionString(true);
				conn = new MySqlConnection(connString);
				conn.Open();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Trace.WriteLine(ex.Message);
				throw;
			}
		}

		protected void Close()
		{
			try
			{
				// delete the table we created.
				if (conn.State == ConnectionState.Closed)
					conn.Open();
				execSQL("DROP TABLE IF EXISTS Test");
				conn.Close();
			}
			catch (Exception ex)
			{
				Assert.Fail(ex.Message);
			}
		}

		protected bool Is50
		{
			get
			{
				string v = conn.ServerVersion;
				return v.StartsWith("5.0") || v.StartsWith("5.1");
			}
		}

		protected bool Is41
		{
			get { return conn.ServerVersion.StartsWith("4.1"); }
		}

		protected bool Is40
		{
			get { return conn.ServerVersion.StartsWith("4.0"); }
		}

		[SetUp]
		protected virtual void Setup()
		{
			try
			{
				IDataReader reader = execReader("SHOW TABLES LIKE 'Test'");
				bool exists = reader.Read();
				reader.Close();
				if (exists)
					execSQL("TRUNCATE TABLE Test");
				if (Is50)
				{
					execSQL("DROP PROCEDURE IF EXISTS spTest");
					execSQL("DROP FUNCTION IF EXISTS fnTest");
				}
			}
			catch (Exception ex)
			{
				Assert.Fail(ex.Message);
			}
		}

		[TearDown]
		protected virtual void Teardown()
		{
			if (Is50)
			{
				execSQL("DROP PROCEDURE IF EXISTS spTest");
				execSQL("DROP FUNCTION IF EXISTS fnTest");
			}
		}

		protected void KillConnection(MySqlConnection c)
		{
			int threadId = c.ServerThread;
			MySqlCommand cmd = new MySqlCommand("KILL " + threadId, conn);
			try
			{
				cmd.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				Assert.Fail(ex.Message);
			}
			c.Ping();  // this final ping will cause MySQL to clean up the killed thread
		}

		protected void createTable(string sql, string engine)
		{
			if (Is41 || Is50)
				sql += " ENGINE=" + engine;
			else
				sql += " TYPE=" + engine;
			execSQL(sql);
		}

		protected void execSQL(string sql)
		{
			MySqlCommand cmd = new MySqlCommand(sql, conn);
			cmd.ExecuteNonQuery();
		}

		protected IDataReader execReader(string sql)
		{
			MySqlCommand cmd = new MySqlCommand(sql, conn);
			return cmd.ExecuteReader();
		}

	}
}
