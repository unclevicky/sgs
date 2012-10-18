﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Sanguosha.Core.Cards;
using Sanguosha.Core.Players;
using Sanguosha.Core.Skills;
using Sanguosha.Core.Games;
using System.Diagnostics;

namespace Sanguosha.Core.Network
{
    public class Client
    {
        NetworkStream stream;
        ItemReceiver receiver;
        ItemSender sender;
        int commId;
        public void Start()
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345);
            TcpClient client = new TcpClient();
            client.Connect(ep);
            stream = client.GetStream();
            receiver = new ItemReceiver(stream);
            sender = new ItemSender(stream);
            commId = 0;
        }

        public object Receive()
        {
            object o = receiver.Receive();
            if (o is int)
            {
                Trace.TraceInformation("Received a {0}", (int)o);
            }
            else
            {
                Trace.TraceInformation("Received a {0}", o.GetType().Name);
            }
            return o;
        }

        public void AnswerNext()
        {
            sender.Send(new CommandItem() { command = Command.QaId, data = commId });
            commId++;
        }

        public void AnswerItem(object o)
        {
            sender.Send(o);
        }
    }
}
