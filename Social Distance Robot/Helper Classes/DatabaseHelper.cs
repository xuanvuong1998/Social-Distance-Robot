using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace robot_head
{
    class DatabaseHelper
    {
        internal class LocationDesp
        {
            public string Name { get; set; }
            public string Desp { get; set; }
        }

        internal class LocationDespDB
        {
            static string _path = System.Windows.Forms.Application.StartupPath;
            static string _file = @"\RobotLocation.accdb";
            static OleDbCommand _cmd = new OleDbCommand();
            static OleDbConnection _conn = new OleDbConnection();
            private static List<LocationDesp> list = null;
            private static List<string> rovingLocationsList = null;


            public static List<LocationDesp> GetLocationDespList()
            {
                if (list == null)
                {
                    LoadDespList();
                }
                return list;
            }
            public static string[] GetRovingLocations()
            {
                if (list == null)
                {
                    LoadRovingLocationsList();
                }

                var finalList = rovingLocationsList[0].Split('/');

                return finalList;
            }

            public static string GetDespByName(string name)
            {
                if (list == null)
                {
                    LoadDespList();
                }

                var foundRes = list.SingleOrDefault(item => item.Name.Equals(name));
                return foundRes?.Desp;
            }

            public static void LoadDespList()
            {
                try
                {
                    _conn.Open();


                    _cmd.CommandText = "select * from Location";

                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(_cmd))
                    {
                        using (var ds = new DataSet())
                        {
                            adapter.Fill(ds);

                            for (int i = 0; i <= ds.Tables[0].Rows.Count - 1; i++)
                            {
                                string name = ds.Tables[0].Rows[i].ItemArray[0] as string;
                                string desp = ds.Tables[0].Rows[i].ItemArray[1] as string;

                                if (list == null) list = new List<LocationDesp>();

                                LocationDesp ld = new LocationDesp
                                {
                                    Name = name,
                                    Desp = desp
                                };

                                list.Add(ld);
                            }
                        }

                    }

                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                }
                finally
                {
                    _conn.Close();
                }
            }
            public static void LoadRovingLocationsList()
            {
                try
                {
                    _conn.Open();


                    _cmd.CommandText = "select * from RovingLocations";

                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(_cmd))
                    {
                        DataSet ds = new DataSet();

                        adapter.Fill(ds);

                        for (int i = 0; i <= ds.Tables[0].Rows.Count - 1; i++)
                        {
                            string name = ds.Tables[0].Rows[i].ItemArray[0] as string;


                            if (rovingLocationsList == null) rovingLocationsList = new List<string>();


                            rovingLocationsList.Add(name);
                        }
                    }

                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                }
                finally
                {
                    _conn.Close();
                }
            }
            static LocationDespDB()
            {
                //_path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];
                _path += @"\Database";
                _conn.ConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;
                                           Persist Security Info=False;
                                           Data Source =" + _path + _file;
                _cmd.Connection = _conn;
            }


        }
    }
}
