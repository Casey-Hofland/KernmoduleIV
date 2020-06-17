using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Handles all UI stuff inside the Joining Area.
public class JoiningAreaUIBinder : MonoBehaviour
{
    [SerializeReference] private PlayerUIField playerUIField;
    [SerializeField] private Transform roomArea;
    [SerializeField] private TMP_Text addressField;
    [SerializeField] private GameObject[] activeWhenJoined;
    [SerializeField] private GameObject[] deactiveWhenJoined;
    [SerializeField] private Button startButton;
    [SerializeField] private GridSettings gridSettings;

    private Dictionary<int, GameObject> spawnedUIFields = new Dictionary<int, GameObject>();
    private Coroutine updateStartButtonRoutine;

    public string uiName { get; set; }
    public string uiAddress { get; set; }

    private void Start()
    {
        roomArea.gameObject.SetActive(false);
        Joined(false);
        startButton.gameObject.SetActive(false);
    }

    // Method called by button when creating a new room. Creates a server and adds a local client.
    public void CreateRoom()
    {
        if(!FindObjectOfType<Server>())
        {
            var serverGameObject = new GameObject(typeof(Server).Name);
            DontDestroyOnLoad(serverGameObject);
            var server = serverGameObject.AddComponent<Server>();
            server.CreateDriver();

            AddClient(null);
        }
    }

    // Method called by button when joining a room. Creates a client that is looking for a host on the specified address and updates UI accordingly.
    public void AddClient() => AddClient(uiAddress);
    public void AddClient(string address)
    {
        if(!FindObjectOfType<Client>())
        {
            var clientGameObject = new GameObject(typeof(Client).Name);
            DontDestroyOnLoad(clientGameObject);
            var client = clientGameObject.AddComponent<Client>();
            client.playerInfo.name = uiName;
            client.CreateDriver();
            client.Connect(address);

            client.onMessageReceived += ClientMessageReceived;
            client.onDisconnected += ClientDisconnected;

            // Show the connected address.
            if(!string.IsNullOrWhiteSpace(address))
            {
                addressField.text = address;
            }
            else
            {
                IPHostEntry host;
                string localIP = "0.0.0.0";
                host = Dns.GetHostEntry(Dns.GetHostName());
                foreach(IPAddress ip in host.AddressList)
                {
                    if(ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIP = ip.ToString();
                        break;
                    }
                }

                addressField.text = localIP;
            }
        }

        // Can be used to instantiate multiple clients when working in the Editor.
#if UNITY_EDITOR
        else
        {
            var clientGameObject = new GameObject(typeof(Client).Name);
            DontDestroyOnLoad(clientGameObject);
            var client = clientGameObject.AddComponent<Client>();
            client.name = uiName;
            client.CreateDriver();
            client.Connect(address);
        }
#endif
    }

    // When a client receives a message, update the UI accordingly.
    private void ClientMessageReceived(Client client, INetworkMessage iMessage)
    {
        switch(iMessage)
        {
            // Show the roomArea and spawn the players PlayerUIField.
            case WelcomeMessage welcomeMessage:
                roomArea.gameObject.SetActive(true);
                Joined(true);
                if(updateStartButtonRoutine == null)
                {
                    updateStartButtonRoutine = StartCoroutine(ToggleStartButtonActivasion());
                }
                SpawnPlayerUIField(welcomeMessage.playerID, welcomeMessage.color, client.playerInfo.name);
                break;
            // Spawn someone elses PlayerUIField.
            case NewPlayerMessage newPlayerMessage:
                SpawnPlayerUIField(newPlayerMessage.playerID, newPlayerMessage.color, newPlayerMessage.name);
                break;
            // Remove someone elses PlayerUIField.
            case PlayerMessage playerMessage when playerMessage.messageType == MessageType.PlayerLeft:
                RemovePlayerUIField(playerMessage.playerID);
                break;
        }
    }

    // A player UI field displays a players color and name.
    private void SpawnPlayerUIField(int id, Color color, string name)
    {
        var spawnedUIField = Instantiate(playerUIField, roomArea);
        spawnedUIFields.Add(id, spawnedUIField.gameObject);
        spawnedUIField.SetColor(color);
        spawnedUIField.SetName(name);
    }

    private void RemovePlayerUIField(int id)
    {
        if(spawnedUIFields.ContainsKey(id))
        {
            Destroy(spawnedUIFields[id]);
            spawnedUIFields.Remove(id);
        }
    }

    // When a player joins or leaves, update the UI accordingly.
    private void Joined(bool value)
    {
        foreach(var gameObject in activeWhenJoined)
        {
            gameObject.SetActive(value);
        }
        foreach(var gameObject in deactiveWhenJoined)
        {
            gameObject.SetActive(!value);
        }
    }

    // If this is the server, constantly check to see if there are a valid amount of players and if there are, activate the start button.
    private IEnumerator ToggleStartButtonActivasion()
    {
        var server = FindObjectOfType<Server>();

        while(server)
        {
            // If this is the Unity Editor, simply keep the start button activated.
#if UNITY_EDITOR
            startButton.gameObject.SetActive(true);
#else
            startButton.gameObject.SetActive(server.connectionsLength >= Server.defaultMinConnections);
#endif
            yield return null;
        }
    }

    private void ClientDisconnected(Client client)
    {
#if UNITY_EDITOR
        RemovePlayerUIField(client.playerID);
#else
        LeaveRoom();
#endif
    }

    // Method called by button when leaving the room. Destroys all drivers and updates UI accordingly.
    public void LeaveRoom()
    {
        // Remove the roomArea and the startButton.
        roomArea.gameObject.SetActive(false);
        Joined(false);
        if(updateStartButtonRoutine != null)
        {
            StopCoroutine(updateStartButtonRoutine);
            updateStartButtonRoutine = null;
        }
        startButton.gameObject.SetActive(false);

        foreach(var server in FindObjectsOfType<Server>())
        {
            Destroy(server.gameObject);
        }
        foreach(var client in FindObjectsOfType<Client>())
        {
            Destroy(client.gameObject);
        }

        // Remove all spawnedUIFields.
        foreach(var uiField in spawnedUIFields.Values)
        {
            Destroy(uiField);
        }
        spawnedUIFields.Clear();
    }

    // Method called by button when (trying) starting the game. Waits 4 seconds as a safety net if other connections have not yet loaded.
    public void StartGame()
    {
        var server = FindObjectOfType<Server>();
        if(server && server.TrySendStartMessage())
        {
            var caveQuestManager = server.gameObject.AddComponent<CaveQuestManager>();
            caveQuestManager.StartCoroutine(Coroutine());

            IEnumerator Coroutine()
            {
                yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "Cave Quest");
                yield return new WaitForSecondsRealtime(4f);

                caveQuestManager.GenerateGrid(gridSettings, 1 + ((uint)(Random.value * uint.MaxValue) - 1));
            }
        }
    }

    // Method called by button when viewing highscores. Load the highscores scene.
    public void ViewHighscores()
    {
        LeaveRoom();

        SceneManager.LoadScene("Alltime Highscores");
    }
}
