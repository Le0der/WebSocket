using GameServerTest.Servers;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("开始启动服务器!!");

Server server = new Server("127.0.0.1", 6688);
server.StartServerASync();

Console.ReadKey();
