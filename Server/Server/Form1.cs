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
using System.IO;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Threading.Tasks;

namespace Server  //gniazdo, ktore bedzie nasluchiwac klientow
{
    public partial class Form1 : Form
    {
        public Thread listenThread;
        public Socket acceptedSocket; 
        List<Thread> threadsList = new List<Thread>();
        public Socket localListeningSocket;  //gniazdo nasluchujace klientow, czyli nasz serwer
        public List<User> usersList = new List<User>();

        public Form1()
        {
            InitializeComponent();

            listenThread = new Thread(StartServer); //pod nowy wątek podpinamy metode StartServer
            listenThread.Start(this);
        }

        public bool IfExists(string log, int i)
        {
            string path = "Database.txt";
            if (File.Exists(path))
            {
                using (StreamReader sr = new StreamReader(path, true)) //jesli na nowym obiekcie wykonamy wszystkie akcje w klamrach, plik sie zamyka i zapisuje, wywalając go z pamieći RAM
                {
                    for (var k = 0; k <= 2; k++) //do pominiecia pierwszych linii, z ktorych nie mozna pobrac danych
                    {
                        sr.ReadLine();
                    }
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] data = line.Split('|');
                        if (data[i] == log)
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                
            }
            return false;
        }
        public bool Match(string log, string pass)
        {
            using (StreamReader sr = new StreamReader("Database.txt", true)) //jesli na nowym obiekcie wykonamy wszystkie akcje w klamrach, plik sie zamyka i zapisuje, wywalając go z pamieći RAM
            {
                for (var k = 0; k <= 2; k++) //do pominiecia pierwszych linii, z ktorych nie mozna pobrac danych
                {
                    sr.ReadLine();
                }
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] data = line.Split('|');
                    if (data[1] == log && data[2] == pass)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public void NotLoggedFeedback(User u)
        {
             byte[] feedback = Encoding.ASCII.GetBytes("You have to be logged in to send messages!");
             u.GetSocket().Send(feedback);
        }

        public void AddToListBox()
        {
            using (StreamReader sr = new StreamReader("Database.txt", true)) //jesli na nowym obiekcie wykonamy wszystkie akcje w klamrach, plik sie zamyka i zapisuje, wywalając go z pamieći RAM
            {
                for (var k = 0; k <= 2; k++) //do pominiecia pierwszych linii, z ktorych nie mozna pobrac danych
                {
                    sr.ReadLine();
                }
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] data = line.Split('|');
                    listBox1.Items.Add(data[1]);
                }
            }
        }
        /*
        public Task<string> BadWordsFilterAsync(string content)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://neutrinoapi-bad-word-filter.p.rapidapi.com/bad-word-filter"),
                Headers =
    {
        { "x-rapidapi-key", "f3543aabd0msh22fd38d235a8f72p1b4f9bjsn5c13bea2a8fc" },
        { "x-rapidapi-host", "neutrinoapi-bad-word-filter.p.rapidapi.com" },
    },
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        { "censor-character", "*" },
        { "content", content },
    }),
            };
            using (var response = client.SendAsync(request).Result)
            {
                response.EnsureSuccessStatusCode();
                var body = response.Content.ReadAsStringAsync();
                return body;
            }
        }
        //==========================================================================================
        string content = await BadWordsFilterAsync(markers[1]);
        string[] stringSeparators = new string[] { "\"" };
        string[] result = content.Split(stringSeparators, StringSplitOptions.None);
        string myText = result[3];
        //==========================================================================================
        */
        public bool IsSwearWord(string content)
        {
            string[] words = content.ToLower().Split(new char[0], StringSplitOptions.RemoveEmptyEntries); //.Split(null) tez dziala
            int counter = words.Count();
            string line;
            
            int i = 0;
            while (i < counter)
            {
                using (StreamReader sr = new StreamReader("swear_words.txt", true)) //jesli na nowym obiekcie wykonamy wszystkie akcje w klamrach, plik sie zamyka i zapisuje, wywalając go z pamieći RAM
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (words[i].Contains(line))
                        {
                            return true;
                        }
                    }
                }
            i++;
            }
            return false;
        }
        public string BadWordsFilter(string content)
        {
            string result = null;
            string line;
            bool flag = false;
            string[] words = content.Split(new char[0], StringSplitOptions.RemoveEmptyEntries); //.Split(null) tez dziala
            content = null;
            string tempWord;
            int counter = words.Count();

            int i = 0;
            while (i < counter)
            {
                using (StreamReader sr = new StreamReader("swear_words.txt", true)) //jesli na nowym obiekcie wykonamy wszystkie akcje w klamrach, plik sie zamyka i zapisuje, wywalając go z pamieći RAM
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        flag = false;
                        tempWord = words[i].ToLower(); //zeby dzialo tez jak wpisze duzymi i malymi literami
                        if (tempWord.Contains(line))
                        {
                            int lenght = tempWord.Length;
                            for (int k = 0; k < lenght; k++)
                            {
                                result += '*';
                            }
                            result += ' ';
                            flag = true;
                            break;
                        }
                    }
                    if (flag == false)
                    {
                        result += words[i];
                        result += ' ';
                    }
                    content += result;
                    result = null;
                i++;
                }
            }
            return content;
        }
        public void StartServer(Object form)
        {
            string path = "Database.txt";
            if (File.Exists(path))   
                AddToListBox();
            RichTextBox brd = ((Form1)form).richTextBox1;

            IPHostEntry host = Dns.GetHostEntry("localhost"); //127.0.0.1 - wskaznik na sam siebie
            IPAddress ipAddress = host.AddressList[0]; //pobieramy lokalny adres ip (akurat tutaj ipv6)
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 50000); //EndPoint


            ((Form1)form).localListeningSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp); //tworzymy nowy socket
            ((Form1)form).localListeningSocket.Bind(localEndPoint); //po stworzeniu gniazda, mowimy web socketowi, ze bedzie podpiete pod localEndPoint, czyli pod lokalny adres ip naszego komputera i ze bedzie nasluchiwac na porcie 50000
            ((Form1)form).localListeningSocket.Listen(10); //w kolejce max. 10 klientow
            /*
            using (StreamWriter baza = new StreamWriter("Baza użytkowników.txt", true)) //jesli na nowym obiekcie wykonamy wszystkie akcje w klamrach, plik sie zamyka i zapisuje, wywalając go z pamieći RAM
            {
                baza.WriteLine("|  Login  |  Encrypted Password  |\n\n");
            }
            */
            // w petli while true
            while (true)
            {
                ((Form1)form).acceptedSocket = ((Form1)form).localListeningSocket.Accept();  // dodaj do lsity  socketów //serwer akceptuje chec nawiazania polaczenia klienta /// .accept() zwroci nowy web socket obslugujacy klienta - zostanie stworzony tunel komunikacyjny z klientem
                //watek sie zatrzyma i bedzie tkwil na linii powyzej dopoki jakis klient sie nie zglosi, czyli dopoki ktos sprobuje sie podlaczyc(dopoki nie wywola metody .connect() do naszego EndPoint)
                //jak sie uda polaczyc to zaakceptuje klienta i pod acceptedsocket zostanie przypisane nowe gniazdo zwrocone przez metode .accept()
                User user = new User(acceptedSocket);
                                                                              
                Thread clientThread = new Thread(() => ((Form1)form).ClientListening(form, user, usersList, brd));  //nowy watek na jednego klienta
                ((Form1)form).threadsList.Add(clientThread); //ddoanie klientow do osobnych wątkow
                clientThread.Start();    
            }
            //string data = null;
            //byte[] bytes = null;
        }

        public /*async*/ void ClientListening(Object form, User user, List<User> usersList, RichTextBox brd)
        {
            /// zamknac w nowym wątku / jeden watek na jednego klienta //nasluchiwanie info od klientow
            /// 

            try
            {
                while (true)
                {
                    string data = null;
                    byte[] bytes = null;

                    bytes = new byte[1024]; //nowe bajty danych
                    int bytesRec = user.GetSocket().Receive(bytes); //watek tez sie zatrzyma i bedzie czekal na to az klient cos nada, potem .receive() przechwytuje go i wypelnia przechwyconymi bajtami zmienna bytes
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec); //GetString - konwersja otrzymanych bajtow na stringa, (jesli byly kodowane w ascii)

                    string[] markers = data.Split('|');
                    string marker = markers[0]; // [0] - znacznik/all/pm, [1] - login/wiadomosc, [2] - zahashowane haslo, 
                    
                    switch (marker)
                    {
                        case "register":
                        {
                                bool flagLog = IfExists(markers[1], 1); // [1] - login, [2] - zahashowane haslo
                                bool tempFlag;
                                bool isBad = IsSwearWord(markers[1]);

                                if (isBad == false)
                                {
                                    if (flagLog)
                                    {
                                        if (user.IsRegistered() == true)
                                        {
                                            byte[] error1 = Encoding.ASCII.GetBytes("You've arleady registered!");
                                            user.GetSocket().Send(error1);
                                        }
                                        else
                                        {
                                            byte[] error2 = Encoding.ASCII.GetBytes("Such user already exists!");
                                            user.GetSocket().Send(error2);
                                            //MessageBox.Show("Such user already exists!", "Error!"); //na koniec dodac spacje (|  Nick  |), ale to regexem bo inaczej nie dziala if
                                        }
                                    }
                                    else
                                    {
                                        if (user.IsRegistered() == false)
                                        {
                                            tempFlag = false;
                                            //listBox1.Items.Add(markers[1]); //zarejestrowani
                                            if (listBox1.Items.Count == 0)
                                                tempFlag = true;

                                            if (tempFlag == true)
                                            {
                                                using (StreamWriter baza = new StreamWriter("Database.txt", true)) //jesli na nowym obiekcie wykonamy wszystkie akcje w klamrach, plik sie zamyka i zapisuje, wywalając go z pamieći RAM
                                                {
                                                    baza.WriteLine("|  Login  |  Encrypted Password  |\n\n");
                                                }
                                            }
                                            using (StreamWriter baza = new StreamWriter("Database.txt", true)) //jesli na nowym obiekcie wykonamy wszystkie akcje w klamrach, plik sie zamyka i zapisuje, wywalając go z pamieći RAM
                                            {
                                                baza.WriteLine("|" + markers[1] + "|" + markers[2] + "|");
                                            }

                                            user.SetRegistered(true);
                                            user.SetLogin(markers[1]);
                                            user.SetPassword(markers[2]);
                                            usersList.Add(user);  //dodanie socketow klientow do listy
                                            byte[] msg = Encoding.ASCII.GetBytes("Registration succeded!");
                                            user.GetSocket().Send(msg);
                                            //MessageBox.Show("Registration succeded!"); //zrobic zeby wiadomosc sie pokazala po stronie klienta a nie serwera, czyli .send()
                                            listBox1.Items.Add(markers[1]); //zarejestrowani
                                        }
                                        else
                                        {
                                            byte[] error = Encoding.ASCII.GetBytes("You've arleady registered!");
                                            user.GetSocket().Send(error);
                                            //MessageBox.Show("You've arleady registered!", "Error!");
                                        }
                                    }
                                }
                                else
                                {
                                    byte[] bad = Encoding.ASCII.GetBytes("Login cannot contain a swear word!");
                                    user.GetSocket().Send(bad);
                                }
                        break;
                        }
                        case "login":
                            {
                                bool flagLog = IfExists(markers[1], 1); // [1] - login, [2] - zahashowane haslo
                                bool flagPass = IfExists(markers[2], 2);
                                bool flagMatch = Match(markers[1], markers[2]);
                                bool flag = false;

                                if (flagLog == false)
                                {
                                    byte[] error = Encoding.ASCII.GetBytes("Such user does not exist! Please register.");
                                    user.GetSocket().Send(error);
                                    //MessageBox.Show("Such user does not exist! Please register.", "Error!"); //zrobic .send() jak wyzej
                                    break;
                                }
                                else if (/*markers[1] != user.GetLogin() || markers[2] != user.GetPassword()*/(flagLog == false || flagPass == false) && user.IsLogged() == false)
                                {
                                    byte[] error = Encoding.ASCII.GetBytes("Improper login or password!");
                                    user.GetSocket().Send(error);
                                    //MessageBox.Show("Improper login or password!", "Error!"); //zrobic .send() jak wyzej
                                    break;
                                    //flag = true;
                                }
                                else if (flagMatch == true)
                                {
                                    if (user.IsLogged() == true)
                                    {
                                        byte[] error = Encoding.ASCII.GetBytes("You've already logged in!");
                                        user.GetSocket().Send(error);
                                        flag = true;
                                    }
                                    if (flag == false)
                                    {
                                        foreach (User element in usersList)
                                        {
                                            if (markers[1] == element.GetLogin() && element.IsLogged() == true)
                                            {
                                                byte[] error = Encoding.ASCII.GetBytes("Such user has already logged in!");
                                                user.GetSocket().Send(error);
                                                //MessageBox.Show("Such user has already logged in!", "Error!"); //na koniec dodac spacje (|  Nick  |), ale to regexem bo inaczej nie dziala if, .send() jak wyzej
                                                flag = true;
                                                break;
                                            }
                                            else
                                                flag = false;
                                        }
                                    }
                                }

                                if ((flag == false /*&& markers[1] == user.GetLogin()*/) /*nie bylo tego warunku jak nie mozna bylo sie logowac z innej formy->*/ /*|| (flag == false && markers[1] != user.GetLogin())*/) 
                                {
                                    foreach (User element in usersList)
                                    {
                                        if (element.GetLogin() == markers[1])
                                        {
                                            user.SetLogin(element.GetLogin());
                                            user.SetPassword(element.GetPassword());
                                            user.SetLogged(true);
                                            user.SetRegistered(element.IsRegistered());
                                            user.AddInbox(element.GetInbox());
                                            usersList.Remove(element);
                                            usersList.Add(user);
                                            flag = true;
                                            break;
                                        }
                                    }
                                    if (flag == false) //zeby dzialalo jak wylacze serwer i bede sie chcial na nowo zalogowac
                                    {
                                        user.SetLogin(markers[1]);
                                        user.SetPassword(markers[2]);
                                        user.SetLogged(true);
                                        user.SetRegistered(true);
                                        usersList.Add(user);
                                    }
                                    //user.SetLogged(true);
                                    //if (!listBox2.Items.Contains(markers[1]))

                                    listBox2.Items.Add(markers[1]); //zalogowani

                                    byte[] msg = Encoding.ASCII.GetBytes("#====================================#" + "\nLogging in succeeded! Hello " + markers[1]/*bylo to jak nie mozna bylo sie logowac z innnej formy: user.GetLogin()*/ + "! \n#====================================#\n");
                                    user.GetSocket().Send(msg);
                                    byte[] hello = Encoding.ASCII.GetBytes(/*user.GetLogin()*/markers[1] + " has just logged in. Enjoy your stay " + markers[1] + "!");
                                    brd.Text += Environment.NewLine + /*user.GetLogin()*/markers[1] + " has just logged in. Enjoy your stay " + markers[1] + "!";
                                    foreach (User element in usersList)
                                    {
                                        if (element.GetLogin() != /*user.GetLogin()*/markers[1] && element.IsLogged() == true) //do wysylania powiadomienia o nowym userze ale nie do tego ktory sie zalogowal
                                        {
                                            element.GetSocket().Send(hello);
                                        }
                                    }
                                    foreach (User element in usersList)
                                    {
                                        if (element.GetLogin() == user.GetLogin() && element.GetInbox().Any())
                                        {
                                            string received = string.Join("\n", element.GetInbox());
                                            byte[] inboxMsg = Encoding.ASCII.GetBytes("INBOX|" + received);
                                            element.GetSocket().Send(inboxMsg);
                                            break;
                                        }
                                    }
                                }/* to bylo jak nie mozna bylo sie logowac z innej formy na zarejestrowane konto
                                else if (flag == false && markers[1] != user.GetLogin())
                                {
                                    byte[] error = Encoding.ASCII.GetBytes("Improper login or password!");
                                    user.GetSocket().Send(error);
                                    //MessageBox.Show("Improper login or password!", "Error!");
                                }*/
                                break;
                            }
                        case "all":
                            {
                                if (user.IsLogged() == false)
                                    NotLoggedFeedback(user);
                                else if(user.IsLogged() == true)
                                {
                                    string censoredText = BadWordsFilter(markers[1]);
                                    byte[] msg = Encoding.ASCII.GetBytes(user.GetLogin() + ": " + censoredText);
                                    foreach (User element in usersList)
                                    {
                                        if (element.IsLogged() == true /*&& usersList.Count() != 0*/)
                                            element.GetSocket().Send(msg);
                                    }
                                    brd.Text += Environment.NewLine + user.GetLogin() + ": " + censoredText; //  nie musi byc / tylko do pokazania  działania
                                }
                                break;
                            }
                        case "pm":
                            {
                                string censoredText = BadWordsFilter(markers[2]);
                                byte[] msgFrom = Encoding.ASCII.GetBytes("<<PM from " + user.GetLogin() + ": " + censoredText);
                                bool flag = false;
                                if (user.IsLogged() == false)
                                    NotLoggedFeedback(user);
                                else
                                {
                                    try
                                    {
                                        foreach (User element in usersList)
                                        {
                                            if (element.GetLogin() == markers[1] && element.IsLogged() == true)
                                            {
                                                element.GetSocket().Send(msgFrom);
                                                byte[] msgTo = Encoding.ASCII.GetBytes(">>PM to " + element.GetLogin() + ": " + censoredText);
                                                user.GetSocket().Send(msgTo);
                                                flag = true;
                                                break;
                                            }
                                            else if(element.GetLogin() == markers[1] && element.IsLogged() == false)
                                            {
                                                element.AddToInbox(Encoding.ASCII.GetString(msgFrom));
                                                byte[] msgTo = Encoding.ASCII.GetBytes("PM_OFFLINE|>>PM to " + element.GetLogin() + "(offline)" + ": " + censoredText);
                                                user.GetSocket().Send(msgTo);
                                                flag = true;
                                                break;
                                            }
                                        }
                                        //flag = true; //naprawic bo nie wchodzi na true i zawsze jest false
                                    }
                                    catch (Exception e)
                                    {
                                        MessageBox.Show(e.Message);
                                    }
                                    if (flag == false)
                                    {
                                        byte[] error = Encoding.ASCII.GetBytes("Destination user does not exist!");
                                        user.GetSocket().Send(error);
                                        //MessageBox.Show("Destination user does not exist or is not logged in!", "Error!");
                                    }
                                }
                                break;
                            }
                            case "Bye bye!":
                            {
                                user.GetInbox().Clear();
                                if (user.IsLogged() == true)
                                {
                                    byte[] ByeBye = Encoding.ASCII.GetBytes("#====================================#\n" + user.GetLogin() + " has just logged out.\nBye bye! Have a nice day " + user.GetLogin() + "!\n#====================================#");
                                    foreach (User element in usersList)
                                    {
                                        if (element.IsLogged() == true && element.GetLogin() != user.GetLogin())
                                            element.GetSocket().Send(ByeBye);
                                    }
                                    brd.Text += Environment.NewLine + "#====================================#\n" + user.GetLogin() + " has just logged out.\nBye bye! Have a nice day " + user.GetLogin() + "!\n#====================================#\n";
                                    break;
                                }
                                else
                                {
                                    byte[] logoutFeedback = Encoding.ASCII.GetBytes("<NOT_LOGGED>");
                                    user.GetSocket().Send(logoutFeedback);
                                    break;
                                }
                            }
                    }
                    //if (data != "<EOF>")
                        //brd.Text += Environment.NewLine + data;

                    if (data.IndexOf("Bye bye!") > -1 && user.IsLogged() == true) //sprawdzenie czy doszlo do konca komunikacji (w odebranym tekscie)
                    {
                        break;
                    }

                    // przeiterowac po liscie  gniazd klientow i wysłac do kazdego odebrana wiadomość tak jak w button1_click
                    
                }
            }
            catch (Exception Error)
            {
                MessageBox.Show(Error.Message); ;
            }
            ///
            ///wiadomosc od jednego klienta jest rozgłaszana do WSZYSTKICH KLIENTÓW
            ///
            byte[] byebyeMessage = Encoding.ASCII.GetBytes("<EOF>");
            user.GetSocket().Send(byebyeMessage);

            //usersList.Remove(socket);
            if (listBox2.Items.Contains(user.GetLogin()))
                listBox2.Items.Remove(user.GetLogin());

            //usersList.Remove(user); 
            user.SetLogged(false);
            user.GetSocket().Shutdown(SocketShutdown.Both); //zamykanie gniazd sieciowych, konczy sie komunikacja
            user.GetSocket().Close();
            user.GetSocket().Dispose();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            byte[] ByeByeAll = Encoding.ASCII.GetBytes("<All.EOF>");
            if (usersList.Any())
            {
                foreach (User element in usersList)
                {
                    //element.GetSocket().Send(ByeByeAll);
                    if (element.IsLogged() == true)
                    {
                        element.GetSocket().Send(ByeByeAll);
                        //Thread.Sleep(500);
                        element.SetLogged(false);
                    }
                }
            }
            //Thread.Sleep(5000);
            //System.Windows.Forms.Application.ExitThread();
            //listBox1.Items.Clear();
            //this.listenThread.Abort();
            this.Close();
        }
    }
}
