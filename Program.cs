using Azure;
using Azure.Communication;
using Azure.Communication.Administration;
using Azure.Communication.Chat;
using Azure.Communication.Identity;
using System;
using System.Threading.Tasks;

namespace acsdemo1
{
    class Program
    {
        private static string ConnectionString = "endpoint=https://ACS-INSTANCE.communication.azure.com/;accesskey=ACCESSKEY";
        private static CommunicationIdentityClient client = new CommunicationIdentityClient(ConnectionString);
        private static string token;
        private static bool tokenset;
        private static string userid;
        private static string displayname;

        static async Task Main(string[] args)
        {
            await ShowMenu();
        }

        /// <summary>
        /// ShowMenu() - main menu
        /// </summary>
        /// <returns></returns>
        private async static Task ShowMenu()
        {
            Console.WriteLine("\n# Azure Communication Services - Demo\n");

            Console.WriteLine($"# Token: {tokenset}");
            Console.WriteLine($"# User: {userid}");
            Console.WriteLine($"# Display name: {displayname}");

            Console.WriteLine("\nSelect option: ");
            Console.WriteLine(" 0) Set token");
            Console.WriteLine(" 1) Set user");
            Console.WriteLine(" 2) Create new user and token");
            Console.WriteLine(" 3) Create new chat thread");
            Console.WriteLine(" 4) Add user to an existing chat thread");
            Console.WriteLine(" 5) Send message to a thread");
            Console.WriteLine(" 6) View messages in a thread");
            Console.WriteLine(" 7) View members in a thread");
            Console.WriteLine(" 8) View all threads");
            Console.WriteLine(" 9) Exit");

            Console.Write("Enter choice: ");
            var choice = Console.ReadKey();

            switch (choice.KeyChar.ToString())
            {
                case "0":
                    SetToken();
                    await ShowMenu();
                    break;
                case "1":
                    SetUser();
                    await ShowMenu();
                    break;
                case "2":
                    await CreateNewUser();
                    await ShowMenu();
                    break;
                case "3":
                    await CreateNewChatThread();
                    await ShowMenu();
                    break;
                case "4":
                    await AddUserToChatThread();
                    await ShowMenu();
                    break;
                case "5":
                    await SendMessageToChatThreadAsync();
                    await ShowMenu();
                    break;
                case "6":
                    await ViewMessagesInAThread();
                    await ShowMenu();
                    break;
                case "7":
                    await ViewMembersInAThread();
                    await ShowMenu();
                    break;
                case "8":
                    ViewAllThreads();
                    await ShowMenu(); 
                    break;
                case "9":
                    System.Environment.Exit(0);
                    break;
                default:
                    await ShowMenu();
                    System.Environment.Exit(-1);
                    break;
            }

            System.Environment.Exit(-1);
        }

        /// <summary>
        /// SetToken() - sets a user authentication token manually
        /// </summary>
        /// <returns></returns>
        private static string SetToken()
        {
            Console.WriteLine("\n# Setting token");
            Console.Write("Enter token: ");
            token = Console.ReadLine();

            tokenset = true;

            return token;

        }

        /// <summary>
        /// SetUser() - sets a user id to be used for subsequent actions
        /// </summary>
        /// <returns></returns>
        private static string SetUser()
        {
            Console.WriteLine("\n# Setting user");
            Console.Write("Enter user id: ");
            userid = Console.ReadLine();

            Console.Write("Enter display name: ");
            displayname = Console.ReadLine();

            return userid;
        }

        /// <summary>
        /// CreateNewUser() - provision a new user to the system (and get User ID back)
        /// </summary>
        /// <returns></returns>
        private static async Task<string> CreateNewUser()
        {
            Console.WriteLine("\n# Provisioning a user");
            Console.Write("Enter user name: ");
            var username = Console.ReadLine();

            var userResponse = await client.CreateUserAsync();
            var user = userResponse.Value;
            Console.WriteLine($"\nCreated a {username} user with ID: {user.Id}");

            userid = user.Id;
            displayname = username;

            Console.WriteLine("Getting the authentication token");
            //// get token 
            var tokenResponse = await client.IssueTokenAsync(user, scopes: new[] { CommunicationTokenScope.Chat });
            token = tokenResponse.Value.Token;
            var expiresOn = tokenResponse.Value.ExpiresOn;
            Console.WriteLine($"\nIssued a token with 'chat' scope that expires at {expiresOn}:");
            Console.WriteLine($"Token: \n{token}");

            return token;

        }

        /// <summary>
        /// CreateNewChatThread() - provision a new chat thread (like a channel or a group chat)
        /// </summary>
        /// <returns></returns>
        private static async Task<string> CreateNewChatThread()
        {
            Console.WriteLine("\n# Creating a new chat thread");

            Console.WriteLine("Enter thread topic: ");
            var topic = Console.ReadLine();

            Uri endpoint = new Uri("https://ACS-INSTANCE.communication.azure.com/");

            CommunicationUserCredential communicationUserCredential = new CommunicationUserCredential(token);
            ChatClient chatClient = new ChatClient(endpoint, communicationUserCredential);

            var chatThreadMember = new ChatThreadMember(new CommunicationUser(userid))
            {
                DisplayName = displayname
            };

            ChatThreadClient chatThreadClient = await chatClient.CreateChatThreadAsync(topic: topic, members: new[] { chatThreadMember });
            Console.WriteLine($"Chat thread ID: {chatThreadClient.Id}");

            return chatThreadClient.Id;
        }

        /// <summary>
        /// SendMessageToChatThreadAsync() - send a new message to an existing chat thread
        /// </summary>
        /// <returns></returns>
        private static async Task SendMessageToChatThreadAsync()
        {
            Console.WriteLine("\n# Sending a new message to a thread");

            Console.Write("Enter thread id: ");
            var threadId = Console.ReadLine();

            Console.Write("Enter message: ");
            var message = Console.ReadLine();

            Uri endpoint = new Uri("https://ACS-INSTANCE.communication.azure.com/");

            CommunicationUserCredential communicationUserCredential = new CommunicationUserCredential(token);
            ChatClient chatClient = new ChatClient(endpoint, communicationUserCredential);

            ChatThreadClient chatThreadClient = chatClient.GetChatThreadClient(threadId);

            var priority = ChatMessagePriority.Normal;

            SendChatMessageResult sendChatMessageResult = await chatThreadClient.SendMessageAsync(message, priority, displayname);
            string messageId = sendChatMessageResult.Id;

            Console.WriteLine($"Message sent: { messageId}");

        }

        /// <summary>
        /// ViewMessagesInAThread() - view messages in a chat thread
        /// </summary>
        /// <returns></returns>
        private static async Task ViewMessagesInAThread()
        {
            Console.WriteLine("\n# Viewing messages in a thread");

            Console.Write("Enter thread id: ");
            var threadId = Console.ReadLine();

            Uri endpoint = new Uri("https://ACS-INSTANCE.communication.azure.com/");

            CommunicationUserCredential communicationUserCredential = new CommunicationUserCredential(token);
            ChatClient chatClient = new ChatClient(endpoint, communicationUserCredential);

            ChatThreadClient chatThreadClient = chatClient.GetChatThreadClient(threadId);

            AsyncPageable<ChatMessage> allMessages = chatThreadClient.GetMessagesAsync();
            await foreach (ChatMessage message in allMessages)
            {
                Console.WriteLine($"{message.Id}: {message.Sender.Id}: {message.Content}");
            }

        }

        /// <summary>
        /// AddUserToChatThread() - add a new member to an existing chat thread
        /// </summary>
        /// <returns></returns>
        private static async Task AddUserToChatThread()
        {
            Console.WriteLine("# Adding a user to a chat thread");

            Console.Write("Enter thread id: ");
            var threadId = Console.ReadLine();

            Console.Write("Enter user id: ");
            var userToAdd = Console.ReadLine();

            Console.Write("Enter display name: ");
            var userDisplayname = Console.ReadLine();

            Uri endpoint = new Uri("https://ACS-INSTANCE.communication.azure.com/");

            CommunicationUserCredential communicationUserCredential = new CommunicationUserCredential(token);
            ChatClient chatClient = new ChatClient(endpoint, communicationUserCredential);

            var chatThreadMember = new ChatThreadMember(new CommunicationUser(userToAdd))
            {
                DisplayName = userDisplayname
            };

            ChatThreadClient chatThreadClient = chatClient.GetChatThreadClient(threadId);

            await chatThreadClient.AddMembersAsync(members: new[] { chatThreadMember });

            Console.WriteLine($"User {userDisplayname} added to thread");
        }

        /// <summary>
        /// ViewMembersInAThread() - view existing members in a chat thread
        /// </summary>
        /// <returns></returns>
        private static async Task ViewMembersInAThread()
        {
            Console.WriteLine("\n# Viewing members in a thread");

            Console.Write("Enter thread id: ");
            var threadId = Console.ReadLine();

            Uri endpoint = new Uri("https://ACS-INSTANCE.communication.azure.com/");

            CommunicationUserCredential communicationUserCredential = new CommunicationUserCredential(token);
            ChatClient chatClient = new ChatClient(endpoint, communicationUserCredential);

            ChatThreadClient chatThreadClient = chatClient.GetChatThreadClient(threadId);

            AsyncPageable<ChatThreadMember> allMembers = chatThreadClient.GetMembersAsync();
            await foreach (ChatThreadMember member in allMembers)
            {
                Console.WriteLine($"{member.DisplayName}");
            }

        }

        /// <summary>
        /// ViewAllThreads() - see all chat threads provisioned
        /// </summary>
        /// <returns></returns>
        private static void ViewAllThreads()
        {
            Console.WriteLine("\n# Viewing all threads");

            Uri endpoint = new Uri("https://ACS-INSTANCE.communication.azure.com/");

            CommunicationUserCredential communicationUserCredential = new CommunicationUserCredential(token);
            ChatClient chatClient = new ChatClient(endpoint, communicationUserCredential);

            var offset = DateTime.Now.AddDays(-5);

            Pageable<ChatThreadInfo> allThreads = chatClient.GetChatThreadsInfo(offset);

            foreach (ChatThreadInfo thread in allThreads)
            {
                Console.WriteLine($"{thread.Id}: {thread.Topic}");
            }

        }


    }
}
