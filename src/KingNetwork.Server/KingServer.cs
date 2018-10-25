using KingNetwork.Server.Interfaces;
using KingNetwork.Server.PacketHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace KingNetwork.Server
{
    /// <summary>
    /// This class is responsible for management of server.
    /// </summary>
    public class KingServer
    {
        #region private members 	

        /// <summary> 	
        /// The network listener instance. 	
        /// </summary> 	
        public NetworkListener _networkListener { get; private set; }

        /// <summary> 	
        /// The network dictionary list of server handlers. 	
        /// </summary> 	
        private Dictionary<ushort, ServerHandler> _serverHandlers;

        /// <summary> 	
        /// The network dictionary of clients. 	
        /// </summary> 	
        private Dictionary<ushort, IClient> _clients;

        /// <summary> 	
        /// The Server port. 	
        /// </summary> 	
        private ushort _port;

        /// <summary> 	
        /// The counter for generation of client id. 	
        /// </summary> 	
        private int _counter = 0;

        #endregion

        #region properties 	

        #endregion

        #region delegates 	

        /// <summary> 	
        /// The server handler. 	
        /// </summary> 	
        /// <param name="index">The index of connected client.</param>
        /// <param name="data">The bytes data from message.</param>
        public delegate void ServerHandler(ushort index, byte[] data);

        #endregion

        private ushort GetNextClientId() => (ushort)Interlocked.Increment(ref _counter);

        /// <summary>
		/// Creates a new instance of a <see cref="KingServer"/>.
		/// </summary>
        /// <param name="port">The server port.</param>
        public KingServer(ushort port)
        {
            try
            {
                _port = port;
                _clients = new Dictionary<ushort, IClient>();
                _serverHandlers = new Dictionary<ushort, ServerHandler>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}.");
            }
        }

        #region internal methods

        /// <summary>
        /// Method responsible execute the callback of message received from client in server.
        /// </summary>
        /// <param name="client">The client instance.</param>
        /// <param name="data">The bytes data from message.</param>
        internal void OnMessageReceived(IClient client, byte[] data)
        {
            try
            {
                ServerHandler serverHandler;

                Console.WriteLine("OnMessageReceived");

                if (_serverHandlers.TryGetValue(data[0], out serverHandler))
                    serverHandler(client.ID, data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}.");
            }
        }

        /// <summary>
        /// Method responsible execute the callback of client connected in server.
        /// </summary>
        /// <param name="tcpClient">The tcp client object from connected client.</param>
        internal void OnClientConnected(TcpClient tcpClient)
        {
            try
            {

                var client = new Client(GetNextClientId(), tcpClient, OnMessageReceived);

                _clients.Add(client.ID, client);

                Console.WriteLine($"Client connected from {client.IP}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}.");
            }
        }

        /// <summary>
        /// Method responsible for start the async network listener.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the task execution.</param>
        internal async Task StartListenerAsync(CancellationToken cancellationToken)
        {
            _networkListener = new NetworkListener(_port, OnClientConnected);

            while (!cancellationToken.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        #endregion

        #region public methods

        /// <summary>
        /// Method responsible for return one connected client by id.
        /// </summary>
        /// <param name="id">The id of connected client.</param>
        public IClient GetClient(ushort id) => _clients[id];

        /// <summary>
        /// Method responsible for return all connected clients.
        /// </summary>
        public IList<IClient> GetAllClients() => _clients.Values.ToList();

        /// <summary>
        /// Method responsible for put packet handler in the list of packet handlers.
        /// </summary>
        /// <param name="type">The value of packet handler.</param>
        public void PutHandler<T>(ushort packet) where T : PacketHandler, new()
        {
            if (_serverHandlers.ContainsKey(packet))
                _serverHandlers.Remove(packet);

            var handler = new T();

            _serverHandlers.Add(packet, handler.HandleMessageData);
        }

        /// <summary>
        /// Method responsible for start the server.
        /// </summary>
        public void Start()
        {
            try
            {
                var cancellationTokenSource = new CancellationTokenSource();

                var listeningTask = StartListenerAsync(cancellationTokenSource.Token);

                listeningTask.Wait(cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}.");
            }
        }

        /// <summary>
        /// Method responsible for send message to specific connected client.
        /// </summary>
        /// <param name="client">The client instance.</param>
        /// <param name="data">The bytes data from message.</param>
        public void SendMessage(IClient client, byte[] data)
        {
            try
            {
                if (client.HasConnected)
                {
                    client.Stream.Write(data, 0, data.Length);
                    client.Stream.Flush();

                    Console.WriteLine($"Message sended to client {client.ID}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}.");
            }
        }

        /// <summary>
        /// Method responsible for send message to all connected client.
        /// </summary>
        /// <param name="data">The bytes data from message.</param>
        public void SendMessageToAll(byte[] data)
        {
            try
            {
                foreach (var client in GetAllClients().Where(c => c.HasConnected))
                {
                    client.Stream.Write(data, 0, data.Length);
                    client.Stream.Flush();

                    Console.WriteLine($"Message sended to client {client.ID}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}.");
            }
        }

        /// <summary>
        /// Method responsible for send message to all connected client minus one especific client.
        /// </summary>
        /// <param name="client">The client instance.</param>
        /// <param name="data">The bytes data from message.</param>
        public void SendMessageToAllMinus(IClient client, byte[] data)
        {
            try
            {
                foreach (var clientToSend in GetAllClients().Where(c => c.HasConnected && c.ID != client.ID))
                {
                    client.Stream.Write(data, 0, data.Length);
                    client.Stream.Flush();

                    Console.WriteLine($"Message sended to client {client.ID}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}.");
            }
        }

        #endregion
    }
}
