using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace BasicMessagingForm.Code.Clients
{
    public class MSMQFileClient
    {
        public int ID { get; set; }
        public String MyLabel { get; set; }
        public MessageQueue MessageQueue { private get; set; }

        private volatile LinkedList<String> in_messages = new LinkedList<String>();

        //setup dictionary for file
        //setup TO path
        //setup check Start
        //setup check end

        //setup check to recall data


        public bool IS_TRANSFERING { get; set; }
        public bool IS_CONFIRMED { get; set; }  

        private Dictionary<int, byte[]> file_data = new Dictionary<int, byte[]>();




        public void checkMessages()
        {
            if (MessageQueue == null || !MessageQueue.Exists(MessageQueue.Path)) { return; }

            MessageQueue.Formatter = new XmlMessageFormatter(new[] { typeof(String) });

            // Add an event handler for the PeekCompleted event.
            MessageQueue.PeekCompleted += OnPeekCompleted;

            // Begin the asynchronous peek operation.
            MessageQueue.BeginPeek();

            // Remove the event handler before closing the queue
            MessageQueue.PeekCompleted -= OnPeekCompleted;
            MessageQueue.Close();
            MessageQueue.Dispose();
        }

        public String[] getMessages()
        {
            String[] copy_messages;

            lock (in_messages)
            {
                copy_messages = new String[in_messages.Count];

                in_messages.CopyTo(copy_messages, 0);
                in_messages.Clear();
            }

            return copy_messages;
        }

        /// <summary>
        /// Sends a message to the given queue.
        /// </summary>
        /// <param name="text">The test to send.</param>
        /// <param name="outbound_queue">The queue to send to.</param>
        /// <exception cref="Exception">On Abort</exception>
        public void sendMessage(String text, MessageQueue outbound_queue)
        {
            // Create a transaction because we are using a transactional queue.
            using (MessageQueueTransaction transaction = new MessageQueueTransaction())
            {
                try
                {
                    // Create queue object
                    using (MessageQueue queue = outbound_queue)
                    {
                        queue.Formatter = new XmlMessageFormatter();

                        transaction.Begin();
                        queue.Send(text, MyLabel, transaction);
                        transaction.Commit();

                    }
                }
                catch
                {
                    transaction.Abort();
                    throw new Exception();
                }
            }
        }




        public void OnPeekCompleted(Object source, PeekCompletedEventArgs asyncResult)
        {
            // Connect to the queue.
            MessageQueue messageQ = (MessageQueue)source;

            // create transaction
            using (MessageQueueTransaction transaction = new MessageQueueTransaction())
            {
                try
                {
                    transaction.Begin(); //Start reading
                    Message message = messageQ.Receive(transaction);



                    //Add to inbox
                    if (message != null)
                    {
                        lock (in_messages)
                        {
                            in_messages.AddLast(message.Body.ToString());
                        }
                    }

                    transaction.Commit(); //Remove message from Q.
                }
                catch (Exception ex)
                {
                    // on error don't remove message from queue
                    Console.WriteLine(ex.ToString());
                    transaction.Abort();
                }
            }

            // Restart the asynchronous peek operation.
            messageQ.BeginPeek();
        }

    }
}
