using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DedicatedClient
{
    public partial class frmLogin : Form
    {
        Socket client;
        private string passPhrase = "messageprogram";
        public frmLogin()
        {
            InitializeComponent();
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                client.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4444));
            }
            catch (SocketException)
            {
                MessageBox.Show("Failed to connect to server!");
                Environment.Exit(0);
            }
        }
        private void btnLogin_Click(object sender, EventArgs e)
        {
            try
            {
                string data = "LOGIN " + txtName.Text + ";" + StringCipher.Hash(txtPass.Text);
                client.Send(Encoding.UTF8.GetBytes(StringCipher.Encrypt(data, passPhrase)));
                string response = Receive(1024);
                if (response == "OK")
                {
                    Hide();
                    frmMessageClient form = new frmMessageClient(txtName.Text, client);
                    form.ShowDialog();
                    Close();
                }
                else if (response == "BANNED")
                {
                    MessageBox.Show("That account is banned from the server!");
                }
                else if (response == "LOGGEDIN")
                {
                    MessageBox.Show("Someone is already logged into this account!");
                }
                else
                {
                    // THERE IS A PROBLEM WITH CIPHER CUZYOU GET NONSENSE AS RESPONSE
                    MessageBox.Show("Failed to login with those credentials!");
                    txtName.Text = "";
                    txtPass.Text = "";
                }
            }
            catch (SocketException)
            {
                MessageBox.Show("Failed to connect to server!");
                return;
            }

        }
        private void btnRegister_Click(object sender, EventArgs e)
        {
            try
            {
                string data = "REGISTER " + txtName.Text + ";" + StringCipher.Hash(txtPass.Text);
                client.Send(Encoding.UTF8.GetBytes(StringCipher.Encrypt(data, passPhrase)));
                string response = Receive(1024);
                if (response == "OK")
                {
                    Hide();
                    frmMessageClient form = new frmMessageClient(txtName.Text, client);
                    form.ShowDialog();
                    Close();
                }
                else
                {
                    MessageBox.Show("That username is already in use on this server!");
                    txtName.Text = "";
                    txtPass.Text = "";
                }
            }
            catch (SocketException)
            {
                MessageBox.Show("Failed to connect to server!");
                return;
            }
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            client.Send(Encode("EXIT"));
            client.Shutdown(SocketShutdown.Both);
            client.Close();
            Environment.Exit(0);
        }
        private string Receive(int bufferLength)
        {
            byte[] buffer = new byte[bufferLength];
            int size = client.Receive(buffer);
            string data = Decode(buffer);
            return data;
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

        private void FrmLogin_Load(object sender, EventArgs e)
        {

        }
    }
}
