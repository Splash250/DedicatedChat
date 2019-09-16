using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DedicatedClient
{
    public partial class frmMessageClient : Form
    {
        string userName;
        Socket client;
        private string passPhrase = "messageprogram";
        public frmMessageClient(string name, Socket clientSocket)
        {
            InitializeComponent();
            userName = name;
            client = clientSocket;
            StartMessageReceivingLoop();
        }
        private void txtMessage_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                try
                {
                    client.Send(Encode(txtMessage.Text));
                    txtMessage.Text = string.Empty;
                }
                catch (SocketException)
                {
                    MessageBox.Show("The connection to the server lost..");
                    Environment.Exit(0);
                }
            }
        }
        private void AppendTextBox(string value)
        {
            try
            {
                if (InvokeRequired)
                {
                    this.Invoke(new Action<string>(AppendTextBox), new object[] { value });
                    return;
                }
                messageField.Text += value + "\r\n";
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to update text-field...");
                Environment.Exit(0);
            }

        }
        private void StartMessageReceivingLoop()
        {
            Thread thread = new Thread(MessageLoop);
            thread.IsBackground = true;
            thread.Start();
        }
        private void MessageLoop()
        {
            byte[] buffer;
            int size = 0;
            while (true)
            {
                try
                {
                    buffer = new byte[1024];
                    size = client.Receive(buffer);
                    string message = Decode(buffer);
                    if (message.StartsWith("kick"))
                    {
                        MessageBox.Show("You have been kicked from the chat!\r\n\r\nReason:\r\n" + message.Remove(0, 4));
                        Environment.Exit(0);
                    }
                    else if (message != null && message != "")
                    {
                        if (!message.StartsWith(userName) && WindowState == FormWindowState.Minimized)
                        {
                            FlashWindow.Flash(this, 5);
                            SoundPlayer audio = new SoundPlayer(Properties.Resources.mail);
                            audio.Play();
                        }
                        AppendTextBox(message);
                    }
                    else
                    {
                        MessageBox.Show("The connection to the server lost!");
                        Environment.Exit(0);
                    }
                }
                catch (SocketException)
                {
                    MessageBox.Show("The connection to the server lost!");
                    Environment.Exit(0);
                }
            }
        }
        private string Decode(byte[] toBeDecoded)
        {
            //decoding
            StringBuilder sb = new StringBuilder();
            //decode the bytes until it reaches the end or the first null
            foreach (byte b in toBeDecoded)
            {
                if (b == 00)
                {
                    break;
                }
                else
                {
                    sb.Append(Convert.ToChar(b));
                }
            }
            string encodedText = sb.ToString();
            //get the string out of the stream
            string decodedText = StringCipher.Decrypt(encodedText, passPhrase);
            return decodedText;
        }
        private byte[] Encode(string toBeEncoded)
        {
            //simple encode and return function
            string cipher = StringCipher.Encrypt(toBeEncoded, passPhrase);
            byte[] encoder = Encoding.UTF8.GetBytes(cipher);
            return encoder;

        }
    }
}
