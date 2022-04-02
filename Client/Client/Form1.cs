using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Windows.Forms;

namespace Client
{
    public partial class Form1 : Form
    {
        Socket localClientSocket; //gniazdo sieciowe klienta
        Thread myth;
        public Form1()
        {
            InitializeComponent();
            IPHostEntry host = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = host.AddressList[0]; // localhost
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 50000);

            // Create a TCP/IP  socket.    
            localClientSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            //nie bindujemy socketa pod dany port i adres ip bo znamy zdalny EndPoint i chcemy sie polaczyc
            localClientSocket.Connect(remoteEP); //chec polaczenia sie z serwerem ///poczatek web socketu //::1 50000

            richTextBox1.Text = "Socket connected to {0}" + localClientSocket.RemoteEndPoint.ToString();

            myth = new Thread(Listen); //nowy wątek podpinamy pod metode listen
            myth.Start(this);
        }

        private void button1_Click(object sender, EventArgs e) //to all
        {
            if (textBox1.Text != "")
            {
                try
                {
                    byte[] bytes = new byte[1024];
                    // Encode the data string into a byte array.    
                    byte[] msg = Encoding.ASCII.GetBytes("all|" + textBox1.Text);
                    int bytesSent = localClientSocket.Send(msg);
                }
                catch (Exception Error)
                {
                    MessageBox.Show(Error.Message);
                }
                textBox1.Text = null;
            }
        }
        // Release the socket.    
        //senderSocket.Shutdown(SocketShutdown.Both);
        //   senderSocket.Close();
        private void button2_Click(object sender, EventArgs e) //private message
        {
            if (textBox3.Text != "")
            {
                try
                {
                    byte[] bytes = new byte[1024];
                    // Encode the data string into a byte array.    
                    byte[] msg = Encoding.ASCII.GetBytes("pm|" + textBox2.Text + "|" + textBox3.Text);
                    int bytesSent = localClientSocket.Send(msg);
                }
                catch (Exception Error)
                {
                    MessageBox.Show(Error.Message);
                }
                textBox3.Text = null;
            }
        }

        private void button3_Click(object sender, EventArgs e) //register
        {
            try
            {         
                if (textBox4.Text == "" || textBox5.Text == "" || textBox6.Text == "")
                {
                    MessageBox.Show("Login or password cannot be empty!", "Error!");
                    textBox4.Text = null;
                    textBox5.Text = null;
                    textBox6.Text = null;
                }
                else
                {
                    //==================================Hashowanie=========================================
                    byte[] hashBytes;
                    using (System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(textBox5.Text);
                        hashBytes = sha256.ComputeHash(inputBytes);
                    }
                    string hashedPassword = Encoding.UTF8.GetString(hashBytes);
                    //==============================Koniec hashowania======================================

                    byte[] data = Encoding.ASCII.GetBytes("register" + "|" + textBox4.Text + "|" + hashedPassword);

                    if (textBox5.Text == textBox6.Text)
                    {
                        localClientSocket.Send(data);
                        textBox4.Text = null;
                        textBox5.Text = null;
                        textBox6.Text = null;
                    }
                    else
                    {
                        MessageBox.Show("Entered passwords do not match!", "Error!");
                        textBox5.Text = null;
                        textBox6.Text = null;
                    }
                }      
            }
            catch (Exception Error)
            {
                MessageBox.Show(Error.Message);
            }
        }

        private void button4_Click(object sender, EventArgs e) //login
        {
            try
            {
                if (textBox7.Text == "" || textBox8.Text == "")
                {
                    MessageBox.Show("Login or password cannot be empty!", "Error!");
                    textBox7.Text = null;
                    textBox8.Text = null;
                }
                else
                {
                    //==================================Hashowanie=========================================
                    byte[] hashBytes;
                    using (System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(textBox8.Text);
                        hashBytes = sha256.ComputeHash(inputBytes);
                    }
                    string hashedPassword = Encoding.UTF8.GetString(hashBytes);
                    //==============================Koniec hashowania======================================

                    byte[] data = Encoding.ASCII.GetBytes("login" + "|" + textBox7.Text + "|" + hashedPassword);

                    localClientSocket.Send(data);
                    textBox7.Text = null;
                    textBox8.Text = null;
                }
            }
            catch (Exception Error)
            {
                MessageBox.Show(Error.Message);
            }
        }

        public static void Listen(Object form)
        {
            RichTextBox mainBrd = ((Form1)form).richTextBox1;
            RichTextBox pmBrd = ((Form1)form).richTextBox2;
            try
            {
                while (true)
                {
                    string data = null; //nasze wiadomosci
                    byte[] bytes = null; //bajty danych(jakies znaki szesnastkowo)
                    bytes = new byte[1024];
                    bool flagMe = false;
                    bool flagServer = false;
                    bool flagNotLogged = false;
                    bool pmFlag = false;

                    int bytesRec = ((Form1)form).localClientSocket.Receive(bytes); //zatrzymujemy sie tutaj i oczekujemy az serwer cos odesle(caly czas w innym watku dlatego nie bedzie blokowalo interfejsu na ktorym klikamy), watek bedzie czekal az serwer cos odpisze, jak serwer cos odpowie to wpisujemy zawartosc w bajty
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec); //jak wczesniej
                    string[] markers = data.Split('|');
                    string marker = markers[0]; // [0] - znacznik/all/pm, [1] - login/wiadomosc, [2] - zahashowane haslo, 
                    if (marker == "PM_OFFLINE")
                    {
                        pmBrd.Text += markers[1] + Environment.NewLine;
                        pmFlag = true;
                    }
                    else if(marker == "INBOX")
                    {
                        pmBrd.Text += markers[1] + Environment.NewLine;
                        pmFlag = true;
                    }

                    //mainBrd.Text = data;
                    if (data.IndexOf("<EOF>") > -1)
                        flagMe = true;
                    if (data.IndexOf("<All.EOF>") > -1)
                        flagServer = true;
                    if (data.IndexOf("<NOT_LOGGED>") > -1)
                        flagNotLogged = true;

                    if (flagNotLogged == true)
                        mainBrd.Text += Environment.NewLine + "You've not logged in yet!";

                    if (flagMe == true)
                    {
                        mainBrd.Text += Environment.NewLine + "#====================================#\n" + "Logging out succeed!\nYou will be logged out in 5 second!\nBye bye! Have a nice day!\n#====================================#";
                        break;
                    }
  
                    if(flagServer == true)
                    {
                        mainBrd.Text += Environment.NewLine + "#====================================#\n" + "Warning!\nSource server has just ended a connection!\nYou will be logged out in 5 second!\n#====================================#";
                        break;
                    }
                    if (flagNotLogged == false && pmFlag == false)
                        mainBrd.Text += Environment.NewLine + data;
                }
            }
            catch (Exception Error)
            {
                MessageBox.Show(Error.Message);
            }
            Thread.Sleep(5000);
            System.Windows.Forms.Application.ExitThread();
            ((Form1)form).Close();
            //((Form1)form).localClientSocket.Shutdown(SocketShutdown.Both);
            //((Form1)form).localClientSocket.Close();
        }
        private void button5_Click(object sender, EventArgs e) //log out
        {
            byte[] msg = Encoding.ASCII.GetBytes("Bye bye!|");
            int bytesSent = localClientSocket.Send(msg);
            
        }
    }
}