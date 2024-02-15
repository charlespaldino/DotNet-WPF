using System.Windows;
using System.Messaging;
using System;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using BasicMessagingForm.Code.Managers;
using System.Runtime.Remoting.Messaging;
using System.ComponentModel;
using System.Windows.Input;
using BasicMessagingForm.Code.Clients;
using System.Xml.Linq;

namespace BasicMessagingForm
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MSMQManager manager_msmq;
        private MSMQFileClient fileclient_msmq;

        private String username = "Default";
        private bool _disposed = false;

        private const String COMMAND_ENTER_CHAT = "ENTER CHAT";
        private const String COMMAND_LEAVE_CHAT = "LEAVE CHAT";
        private const String COMMAND_GET_MEMBERS = "GET CHAT LIST";
        private const String COMMAND_LIST_MEMBERS = "RECEIVE CHAT LIST";
        private const char COMMAND_DELIM = ':';

        #region "Initialize"
        public MainWindow()
        {
            InitializeComponent();

            manager_msmq = new MSMQManager("BasicMessagingForm_");
            fileclient_msmq = new MSMQFileClient();
        }

        ~MainWindow()
        {
            _disposed = true;
            manager_msmq.Dispose();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            new Thread(getQMessages).Start();
            Title += " FORM:" + manager_msmq.ID;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _disposed = true;
            manager_msmq.sendMessage(COMMAND_LEAVE_CHAT + COMMAND_DELIM + username, username);
            manager_msmq.Dispose();
        }
        #endregion

        #region "Navigation"

        private void tabChat_Clicked(object sender, MouseButtonEventArgs e)
        {
            //Log chat in with settings name
            manager_msmq.sendMessage(COMMAND_ENTER_CHAT + COMMAND_DELIM + username, username);
            manager_msmq.sendMessage(COMMAND_GET_MEMBERS, username);

            //Remove old name.
            removeMember(manager_msmq.ID);

            //Add new name.
            if (!list_chatmembers.Items.Contains(username))
            {
                addMember(username);
            }
            
        }
        #endregion

        #region "Functions"
        private void button_send_Click(object sender, RoutedEventArgs e)
        {
            manager_msmq.sendMessage(username + ": " + textbox_send.Text, MSMQManager.MESSAGE_FROM_LABEL);
            textarea_console.AppendText(username + ": " + textbox_send.Text + "\r");
            textbox_send.Text = "";
        }

        private void textbox_username_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(manager_msmq != null)
            {
                username = textbox_username.Text;
            }
         
        }
        #endregion


        #region "Updating"
        public void getQMessages()
        {
            Console.WriteLine("Start: Getting Q Messages");
            while (!_disposed)
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        Console.WriteLine("Run: Getting Q Messages");
                        manager_msmq.checkMessages();
                        manager_msmq.getMessages().ToList().ForEach(message =>
                        {

                            if(message.StartsWith(COMMAND_ENTER_CHAT))
                            {
                                textarea_console.AppendText(message.Split(COMMAND_DELIM)[1] + " has entered chat.\r");
                                addMember(message.Split(COMMAND_DELIM)[1]);
                            }
                            else if(message.StartsWith(COMMAND_LEAVE_CHAT))
                            {
                                textarea_console.AppendText(message.Split(COMMAND_DELIM)[1] + " has left chat.\r");
                                removeMember(message.Split(COMMAND_DELIM)[1]);
                            }
                            else if (message.StartsWith(COMMAND_GET_MEMBERS))
                            {
                                manager_msmq.sendMessage(COMMAND_LIST_MEMBERS + COMMAND_DELIM + getMembers(), COMMAND_LIST_MEMBERS);
                            }
                            else if (message.StartsWith(COMMAND_LIST_MEMBERS))
                            {
                                foreach (String item in message.Split(COMMAND_DELIM))
                                {
                                    if (item.Equals(COMMAND_LIST_MEMBERS) || item.Equals(username)) { continue; }
                                    else
                                    {
                                        addMember(item);
                                    }
                                }
                            }
                            else
                            {
                                textarea_console.AppendText(message + "\r");
                            }
                        });

                    });
                    Thread.Sleep(100);
                }
                catch (Exception e) 
                {
                    Console.WriteLine(e.Message + Environment.NewLine);
                    Console.WriteLine(e.StackTrace);
                }              
            }
        }

        private void addMember(String name)
        {
            if(!list_chatmembers.Items.Contains(name))
            {
                list_chatmembers.Items.Add(name);
            }
        }

        private void removeMember(String name)
        {
            for (int x = 0; x < list_chatmembers.Items.Count; x++)
            {
                if (list_chatmembers.Items.GetItemAt(x).ToString().Equals(name))
                {
                    list_chatmembers.Items.RemoveAt(x);
                    break;
                }
            }
        }

        private void removeMember(int id)
        {
            for (int x = 0; x < list_chatmembers.Items.Count; x++)
            {
                if (list_chatmembers.Items.GetItemAt(x).ToString().StartsWith(manager_msmq.ID + ""))
                {
                    list_chatmembers.Items.RemoveAt(x);
                    break;
                }
            }
        }

        private String getMembers()
        {
            String members = "";
            for (int x = 0; x < list_chatmembers.Items.Count; x++)
            {
                members += list_chatmembers.Items.GetItemAt(x).ToString() + COMMAND_DELIM;
            }

            return members.TrimEnd(COMMAND_DELIM);
        }

        #endregion

    }


}
