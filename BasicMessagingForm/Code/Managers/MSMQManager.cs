using BasicMessagingForm.Code.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BasicMessagingForm.Code.Managers
{
    public class MSMQManager : IDisposable
    {
        public static String MESSAGE_FROM_LABEL = "Message From {0}";
        private static String FORMAT_PRIVATE_PATH = @".\private$\{0}";


        public int ID { get; private set; }
        private String my_qpath = "";
        private String Name {  get; set; }  

        private Dictionary<int, String> all_Qpaths = new Dictionary<int, String>();

        private volatile LinkedList<String> in_messages = new LinkedList<String>();

        private MSMQClient client { get; set; }

        public MSMQManager(String QPathName)
        {
            Name = QPathName;
            my_qpath = String.Format(FORMAT_PRIVATE_PATH, QPathName);
            discoverMyQPath();

            client = new MSMQClient();
            client.ID = ID;
            client.MyLabel = String.Format(MESSAGE_FROM_LABEL, ID);
            client.MessageQueue = GetQueueReference();

        }

        ~MSMQManager()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (MessageQueue.Exists(my_qpath))
            {
                MessageQueue.Delete(my_qpath); //Free up this slot.
            }
        }


        #region "Initialization"
        /// <summary>
        /// Looks for missing Qs and fills it.
        /// Potential issue for duplicate Qs if one is lost.
        /// </summary>
        private void discoverMyQPath()
        {
            for (int x = 0; x < 100; x++)
            {
                all_Qpaths[x] = my_qpath + x;
            }

            for (int x = 0; x < 100; x++)
            {
                if (!MessageQueue.Exists(my_qpath + x))
                {
                    my_qpath += x; //ID & Path discovery.
                    ID = x;
                    break;
                }
            }
        }

        #endregion

        #region "Client Functions"
        public void checkMessages()
        {
            client.checkMessages();
        }

        public String[] getMessages()
        {
            return client.getMessages();
        }

        /// <summary>
        /// Sends a message to all known existing queues.
        /// </summary>
        public void sendMessage(String text, String label)
        {
            try
            {
                all_Qpaths.Keys.Where(key => key != ID).ToList().ForEach(key => {

                    if(MessageQueue.Exists(String.Format(FORMAT_PRIVATE_PATH, Name) + key))
                    {
                        using (MessageQueue outbound_queue = new MessageQueue(all_Qpaths[key]))
                        {
                            client.sendMessage(text, outbound_queue);
                            Console.WriteLine("Send To: " + key);
                        }
                    }

                });
            }
            catch
            {
                Console.WriteLine("Send: ABORTED");
            }
        }
        #endregion

        #region "Helpers"
        public MessageQueue GetQueueReference()
        {
            return MessageQueue.Exists(my_qpath) ? new MessageQueue(my_qpath) : MessageQueue.Create(my_qpath, true);
        }
        #endregion
    }
}