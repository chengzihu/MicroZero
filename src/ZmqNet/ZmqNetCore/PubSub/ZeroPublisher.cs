using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Agebull.Common;
using Agebull.Common.Logging;
using Agebull.ZeroNet.Core;
using NetMQ;
using NetMQ.Sockets;

namespace Agebull.ZeroNet.PubSub
{
    /// <summary>
    ///     消息发布
    /// </summary>
    public class ZeroPublisher
    {
        /// <summary>
        ///     保持长连接的连接池
        /// </summary>
        private static readonly Dictionary<string, KeyValuePair<StationConfig, RequestSocket>> Publishers =
            new Dictionary<string, KeyValuePair<StationConfig, RequestSocket>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 请求队列
        /// </summary>
        private static readonly SyncQueue<PublishItem> Items = new SyncQueue<PublishItem>();

        /// <summary>
        ///     运行状态
        /// </summary>
        public static StationState RunState { get; private set; }
        /// <summary>
        /// 缓存文件名称
        /// </summary>
        private static string CacheFileName => Path.Combine(StationProgram.Config.DataFolder,
            "zero_publish_queue.json");
        /// <summary>
        ///     启动
        /// </summary>
        public static void Initialize()
        {
            RunState = StationState.Start;
            var old = MulitToOneQueue<PublishItem>.Load(CacheFileName);
            if (old == null)
                return;
            foreach (var val in old.Queue)
                Items.Push(val);
        }
        /// <summary>
        ///     启动
        /// </summary>
        internal static void Start()
        {
            Task.Factory.StartNew(SendTask);
        }
        /// <summary>
        /// 关闭
        /// </summary>
        /// <returns></returns>
        public static bool Stop()
        {
            lock (Publishers)
            {
                foreach (var vl in Publishers.Values)
                    vl.Value.CloseSocket(vl.Key.OutAddress);
                Publishers.Clear();
            }
            //未运行状态
            if (RunState < StationState.Start || RunState > StationState.Failed)
                return true;
            StationConsole.WriteInfo("Publisher closing....");
            RunState = StationState.Closing;
            do
            {
                Thread.Sleep(20);
            } while (RunState != StationState.Closed);
            
            StationConsole.WriteInfo("Publisher closed");
            return true;
        }
        /// <summary>
        /// 发送广播
        /// </summary>
        /// <param name="station"></param>
        /// <param name="title"></param>
        /// <param name="sub"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static void Publish(string station, string title, string sub, string value)
        {
            Items.Push(new PublishItem
            {
                Station = station,
                Title = title,
                SubTitle = sub,
                Content = value ?? "{}"
            });
            if (RunState == StationState.Closed)
                Items.Save(CacheFileName);
        }
        /// <summary>
        /// 广播总数
        /// </summary>
        public static ulong PubCount { get; private set; }
        /// <summary>
        ///     发送广播的后台任务
        /// </summary>
        private static void SendTask()
        {
            RunState = StationState.Run;
            while (StationProgram.State < StationState.Closing && RunState == StationState.Run)
            {
                if (StationProgram.State != StationState.Run)
                {
                    Thread.Sleep(1000);
                    continue;
                }
                if (!Items.StartProcess(out var item, 100))
                    continue;
                if (!GetSocket(item.Station, out var socket, out var status))
                {
                    if (status == ZeroCommandStatus.NoRun)
                    {
                        Thread.Sleep(50);
                        continue;
                    }
                    LogRecorder.Trace(LogType.Error, "Publish",
                        $@"因为无法找到站点而导致向【{item.Station}】广播的主题为【{item.Title}】的消息被遗弃，内容为：
{item.Content}");
                    Items.EndProcess();
                    continue;
                }
                if (!Send(socket, item))
                {
                    LogRecorder.Trace(LogType.Warning, "Publish",
                        $@"向【{item.Station}】广播的主题为【{item.Title}】的消息发送失败，内容为：
{item.Content}");
                    continue;
                }

                Items.EndProcess();
                PubCount++;
            }
            RunState = StationState.Closed;
        }

        /// <summary>
        ///     发送广播
        /// </summary>
        /// <param name="item"></param>
        /// <param name="socket"></param>
        /// <returns></returns>
        private static bool Send(RequestSocket socket, PublishItem item)
        {
            byte[] description = new byte[5];
            description[0] = 3;
            description[1] = ZeroFrameType.Publisher;
            description[2] = ZeroFrameType.SubTitle;
            description[3] = ZeroFrameType.Argument;
            description[4] = ZeroFrameType.End;
            try
            {
                lock (socket)
                {
                    socket.SendMoreFrame(item.Title);
                    socket.SendMoreFrame(description);
                    socket.SendMoreFrame(StationProgram.Config.StationName);
                    socket.SendMoreFrame(item.SubTitle);
                    socket.SendFrame(item.Content);
                    var word = socket.ReceiveFrameString();
                    return word == ZeroNetStatus.ZeroCommandOk;
                }
            }
            catch (Exception e)
            {
                LogRecorder.Exception(e);
                StationConsole.WriteError($"【{item.Station}-{item.Title}】request error =>{e.Message}");
            }
            lock (Publishers)
            {
                socket.CloseSocket(Publishers[item.Station].Key.OutAddress);
                Publishers.Remove(item.Station);
            }

            return false;
        }
        
        /// <summary>
        ///     取得Socket对象
        /// </summary>
        /// <param name="type"></param>
        /// <param name="socket"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        private static bool GetSocket(string type, out RequestSocket socket, out ZeroCommandStatus status)
        {
            if (RunState >= StationState.Closing)
            {
                socket = null;
                status = ZeroCommandStatus.NoRun;
                return false;
            }
            lock (Publishers)
            {
                if (Publishers.TryGetValue(type, out var cs))
                {
                    socket = cs.Value;
                    status = ZeroCommandStatus.Success;
                    return true;
                }
                var config = StationProgram.GetConfig(type, out status);
                if (status == ZeroCommandStatus.NoFind || config == null)
                {
                    socket = null;
                    return false;
                }
                try
                {
                    socket = new RequestSocket();
                    socket.Options.Identity = StationProgram.Config.StationName.ToAsciiBytes();
                    socket.Options.ReconnectInterval = new TimeSpan(0, 0, 0, 0, 200);
                    socket.Connect(config.OutAddress);
                }
                catch (Exception e)
                {
                    LogRecorder.Exception(e);
                    StationConsole.WriteError($"【{type}】connect error =>连接时发生异常：{e}");
                    socket = null;
                    status = ZeroCommandStatus.Exception;
                    return false;
                }
                Publishers.Add(type, new KeyValuePair<StationConfig, RequestSocket>(config, socket));
            }

            return true;
        }
    }
}