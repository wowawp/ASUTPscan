using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Xml.Linq;
using System.Data.SqlClient;
using System.Threading;

namespace ASUTPscan
{

    public partial class Form1 : Form
    {
        const int quantityStrs = 300;//78444;

        public Form1()
        {
            InitializeComponent();
            XMLRead();

        }

        bool isCfgReadSucces = true;
        string ip_st;
        string name_line;
        string name_st;
        string lpszUsername = "admin";
        string lpszPassword = "admin";
        string lpszDomain = "";
        string[] arr = { "" };
        

        //
        // In a using statement, acquire the SqlConnection as a resource.
        //

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            textBox1.ScrollBars = ScrollBars.Vertical;
        }
        private void XMLRead()
        {
            XElement xmlconfig = default(XElement);
            if (File.Exists("config.xml"))
            {
                try
                {
                    xmlconfig = XElement.Load("config.xml");
                    isCfgReadSucces = true;
                }
                catch
                {
                    isCfgReadSucces = false;
                    MessageBox.Show("Ошибка чтения файла конфигурации. Приложение будет закрыто", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }
                IEnumerable<XElement> xmllines =
                    from el in xmlconfig.Elements("line")
                    select el;
                foreach (XElement el in xmllines)
                {

                    try
                    {
                        name_line = el.Attribute("name").Value;
                        textBox1.Text += "Линия: " + name_line.ToString() + Environment.NewLine;
                    }
                    catch
                    {
                        name_line = "";
                    }

                    IEnumerable<XElement> xmlstations =
                        from st in el.Elements("station")
                        select st;

                    int a = 0;


                    foreach (XElement station in xmlstations)
                    {


                        a += 1;
                        name_st = station.Attribute("name").Value;
                        ip_st = station.Attribute("IP").Value;
                        try
                        {
                            for (int i = 0; i < arr.Length; i++)
                                arr[i] = ip_st;
                            foreach (string i in arr)
                                //textBox1.Text += i + Environment.NewLine;

                                textBox1.Text += "  Станция: " + name_st.ToString() + " IP: " + i + " number: " + a + Environment.NewLine;
                        }
                        catch
                        {
                            name_st = "";
                        }
                    }

                }

            }
            else
            {
                textBox1.Text += ("NOT FOUND CFG FILE");
            }
        }


        private void scan_Click(object sender, EventArgs e)
        {
            if (Directory.GetDirectories(@"C:\Users").Length + Directory.GetFiles(@"C:\Users").Length > 0)
            {
                // Папка не пуста
                MessageBox.Show("Папка не пуста");
            }
            else
            {
                // Папка пуста
                MessageBox.Show("Папка пуста");
            }
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword,
        int dwLogonType, int dwLogonProvider, ref IntPtr phToken);

        [DllImport("kernel32.dll")]
        private static extern Boolean CloseHandle(IntPtr hObject);


        private void connect_Click(object sender, EventArgs e)
        {
            string[] arr_ip = { "" };
            textBox1.Text = String.Empty;
            textBox1.Text += ("Старт") + DateTime.Today.ToString("MM/dd/yy H:mm:ss") + Environment.NewLine;
            ThreadPool.QueueUserWorkItem(new WaitCallback((obj) =>
            {
                StringBuilder sb = new StringBuilder();
                int q = 0;
                progressBar1.Value = 0;
                Stopwatch sw = new Stopwatch();
                sw.Start();
  
                string sql = "SELECT * FROM dbo.s_station WHERE ip_s <> ''";
                SqlConnection conn = new SqlConnection("Data Source = 10.239.1.48; Initial Catalog = _096_RMF_POD_TEST; Persist Security Info=True;User ID = test_log; Password=1q2w");
                conn.Open();
                SqlCommand command1 = new SqlCommand(sql, conn);
                SqlDataReader dataReader1 = command1.ExecuteReader();
                int Count = 0;
                while (dataReader1.Read())
                {
                    string stationIP = (dataReader1["ip_s"]).ToString();
                    string stationNAME = (dataReader1["name_st"]).ToString();
                    Count += 1;
                    progressBar1.Invoke(new Action(() => progressBar1.Value += 1));
                    //progressBar1.Value += 1;
                    Ping p = new Ping();
                    PingReply r;
                    r = p.Send(stationIP);

                    if (r.Status == IPStatus.Success)
                    {
                        q += 1;

                        Debug.WriteLine("Cтанция: " + stationNAME + " IP:" + stationIP + q);
                        const int LOGON_TYPE_NEW_CREDENTIALS = 9;
                        const int LOGON32_PROVIDER_WINNT50 = 3;
                        IntPtr token = IntPtr.Zero;
                        LogonUser(lpszUsername, stationIP, lpszPassword, LOGON_TYPE_NEW_CREDENTIALS, LOGON32_PROVIDER_WINNT50,
                    ref token);
                        using (WindowsImpersonationContext person = new WindowsIdentity(token).Impersonate())
                        {
                            try
                            {
                                int qq = 0;
                                textBox1.Invoke(new Action(() => textBox1.Text += ("Проверка: " + stationNAME + Environment.NewLine)));
                                //textBox1.Text += ("Проверка: " + stationNAME + Environment.NewLine);
                                string[] files = Directory.GetFiles(@"\\" + stationIP + @"\DC", "*.rdm", SearchOption.AllDirectories);
                                textBox1.Invoke(new Action(() => textBox1.Text += ("Всего файлов: " + files.Length + Environment.NewLine)));
                                //textBox1.Text += ("Всего файлов: " + files.Length + Environment.NewLine);
                                foreach (string file in files)
                                {
                                    FileInfo info = new FileInfo(file);
                                    if (info.LastWriteTime.ToString("ddMMyyyy") == DateTime.Today.ToString("ddMMyyyy"))
                                    {
                                        //textBox1.Text += (("Станция: " + stationNAME + Environment.NewLine));
                                        textBox1.Invoke(new Action(() => textBox1.Text += (("File: " + Path.GetFileName(file) + " Time: " + info.LastWriteTime.ToString() + Environment.NewLine))));
                                        //textBox1.Text += (("File: " + Path.GetFileName(file) + " Time: " + info.LastWriteTime.ToString() + Environment.NewLine));
                                        qq += 1;
                                    }
                                    else
                                    { 
                                        // Папка пуста
                                    }
                                }
                                textBox1.Invoke(new Action(() => textBox1.Text += ("Кол-во новых фалов: " + qq + Environment.NewLine)));
                                //textBox1.Text += ("Кол-во новых фалов: " + qq + Environment.NewLine);
                            }
                            catch (IOException err)
                            {
                                textBox1.Invoke(new Action(() => textBox1.Text += ("Процесс не удался:" + err.ToString())));
                                //textBox1.Text += ("Процесс не удался:" + err.ToString());
                            }
                            finally
                            {
                                person.Undo();
                                CloseHandle(token);
                            }

                        }
                    }
                    else
                    {
                        q += 1;
                        Debug.WriteLine("Skip");
                    }





                //textBox1.Text += (dataReader1["name_st"]).ToString() + Environment.NewLine;
                //Debug.WriteLine("Данные есть но не отобразились");
                }
                dataReader1.Close();
                conn.Close();
                sw.Stop();
                textBox1.Invoke(new Action(() => textBox1.Text += ((sw.ElapsedMilliseconds / 1000).ToString())));
               // textBox1.Text += ((sw.ElapsedMilliseconds / 1000).ToString());
                textBox1.Invoke(new Action(() => textBox1.Text += "Количество станций:" + Count));
                //textBox1.Text += "Количество станций:" + Count;
            }));
        }

        private void search_Click(object sender, EventArgs e)
        {
            string sql = "SELECT * FROM dbo.s_station";
            SqlConnection conn = new SqlConnection("Data Source = 10.239.1.48; Initial Catalog = _096_RMF_POD_TEST; Persist Security Info=True;User ID = test_log; Password=1q2w");
            conn.Open();
            SqlCommand command1 = new SqlCommand(sql, conn);
            SqlDataReader dataReader1 = command1.ExecuteReader();
            while (dataReader1.Read())
            {
                textBox1.Text += (dataReader1["name_st"]).ToString() + Environment.NewLine;
                
            }
            dataReader1.Close();
            conn.Close();
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {
            
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
          
        }
    }
}


