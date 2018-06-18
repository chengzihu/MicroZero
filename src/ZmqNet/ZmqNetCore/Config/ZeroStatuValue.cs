using Agebull.ZeroNet.ZeroApi;
using Newtonsoft.Json;

namespace Agebull.ZeroNet.Core
{
    /// <summary>
    /// ZeroNet状态值
    /// </summary>
    public static class ZeroStatuValue
    {
        /// <summary>
        /// 正常状态
        /// </summary>
        public const byte ZeroStatusSuccess = (byte) '+';

        /// <summary>
        /// 错误状态
        /// </summary>
        public const byte ZeroStatusBad = (byte) '-';

        /// <summary>
        /// 成功
        /// </summary>
        public const string ZeroCommandOk = "+ok";

        /// <summary>
        /// 计划执行
        /// </summary>
        public const string ZeroCommandPlan = "+plan";

        /// <summary>
        /// 错误
        /// </summary>
        public const string ZeroCommandError = "-error";

        /// <summary>
        /// 正在执行
        /// </summary>
        public const string ZeroCommandRuning = "+runing";

        /// <summary>
        /// 成功退出
        /// </summary>
        public const string ZeroCommandBye = "+bye";

        /// <summary>
        /// 成功加入
        /// </summary>
        public const string ZeroCommandWecome = "+wecome";

        /// <summary>
        /// 投票已发出
        /// </summary>
        public const string ZeroVoteSended = "+send";

        /// <summary>
        /// 投票已关闭
        /// </summary>
        public const string ZeroVoteClosed = "+close";

        /// <summary>
        /// 已退出投票
        /// </summary>
        public const string ZeroVoteBye = "+bye";

        /// <summary>
        /// 投票正在进行中
        /// </summary>
        public const string ZeroVoteWaiting = "+waiting";

        /// <summary>
        /// 投票已开始
        /// </summary>
        public const string ZeroVoteStart = "+start";

        /// <summary>
        /// 投票已完成
        /// </summary>
        public const string ZeroVoteEnd = "+end";

        /// <summary>
        /// 找不到主机
        /// </summary>
        public const string ZeroCommandNoFind = "-no find";

        /// <summary>
        /// 帧错误
        /// </summary>
        public const string ZeroCommandInvalid = "-invalid";

        /// <summary>
        /// 不支持的操作
        /// </summary>
        public const string ZeroCommandNoSupport = "-no support";

        /// <summary>
        /// 执行失败
        /// </summary>
        public const string ZeroCommandFailed = "-failes";

        /// <summary>
        /// 参数错误
        /// </summary>
        public const string ZeroCommandArgError = "-invalid. argument error, must like : call [name] [command] [argument]";

        /// <summary>
        /// 安装时参数错误
        /// </summary>
        public const string ZeroCommandInstallArgError = "-invalid. argument error, must like :install [type] [name]";

        /// <summary>
        /// 执行超时
        /// </summary>
        public const string ZeroCommandTimeout = "-time out";

        /// <summary>
        /// 发生网络异常
        /// </summary>
        public const string ZeroCommandNetError = "-net error";

        /// <summary>
        /// 找不到实际处理者
        /// </summary>
        public const string ZeroCommandNotWorker = "-not work";

        /// <summary>
        /// 未知错误
        /// </summary>
        public const string ZeroUnknowError = "-error";

        /// <summary>
        /// 参数错误的Json文本
        /// </summary>
        /// <remarks>参数校验不通过</remarks>
        public static readonly string SucceesJson = JsonConvert.SerializeObject(ApiResult.Succees());


        /// <summary>
        /// 参数错误的Json文本
        /// </summary>
        /// <remarks>参数校验不通过</remarks>
        public static readonly string ArgumentErrorJson = JsonConvert.SerializeObject(ApiResult.Error(ErrorCode.ArgumentError,"参数错误"));

        /// <summary>
        /// 拒绝访问的Json文本
        /// </summary>
        /// <remarks>权限校验不通过</remarks>
        public static readonly string DenyAccessJson = JsonConvert.SerializeObject(ApiResult.Error(ErrorCode.DenyAccess,"拒绝访问"));

        /// <summary>
        /// 未知错误的Json文本
        /// </summary>
        public static readonly string UnknowErrorJson = JsonConvert.SerializeObject(ApiResult.Error(ErrorCode.UnknowError));

        /// <summary>
        /// 网络错误的Json文本
        /// </summary>
        /// <remarks>调用其它Api时时抛出未处理异常</remarks>
        public static readonly string NetworkErrorJson = JsonConvert.SerializeObject(ApiResult.Error(ErrorCode.NetworkError));

        /// <summary>
        /// 网络超时的Json文本
        /// </summary>
        /// <remarks>调用其它Api时时抛出未处理异常</remarks>
        public static readonly string TimeOutJson = JsonConvert.SerializeObject(ApiResult.Error(ErrorCode.TimeOut));

        /// <summary>
        /// 内部错误的Json文本
        /// </summary>
        /// <remarks>执行方法时抛出未处理异常</remarks>
        public static readonly string InnerErrorJson = JsonConvert.SerializeObject(ApiResult.Error(ErrorCode.InnerError));

        /// <summary>
        /// 页面不存在的Json文本
        /// </summary>
        public static readonly string NoFindJson = JsonConvert.SerializeObject(ApiResult.Error(ErrorCode.NoFind, "页面不存在"));

        /// <summary>
        /// 服务不可用的Json文本
        /// </summary>
        public static readonly string UnavailableJson = JsonConvert.SerializeObject(ApiResult.Error(ErrorCode.Unavailable, "服务不可用"));
        
        /// <summary>
        /// 系统未就绪的Json文本
        /// </summary>
        public static readonly string NoReadyJson = JsonConvert.SerializeObject(ApiResult.Error(ErrorCode.NoReady, "系统未就绪"));

        /// <summary>
        /// 状态原始文本
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static string Text(this ZeroOperatorStateType state)
        {
            switch (state)
            {
                case ZeroOperatorStateType.Ok:
                    return ZeroCommandOk;
                case ZeroOperatorStateType.Plan:
                    return ZeroCommandPlan;
                case ZeroOperatorStateType.Runing:
                    return ZeroCommandRuning;
                case ZeroOperatorStateType.Bye:
                    return ZeroCommandBye;
                case ZeroOperatorStateType.Wecome:
                    return ZeroCommandWecome;
                case ZeroOperatorStateType.VoteSend:
                    return ZeroVoteSended;
                case ZeroOperatorStateType.VoteWaiting:
                    return ZeroVoteWaiting;
                case ZeroOperatorStateType.VoteBye:
                    return ZeroCommandBye;
                case ZeroOperatorStateType.VoteStart:
                    return ZeroVoteStart;
                case ZeroOperatorStateType.VoteClose:
                    return ZeroVoteClosed;
                case ZeroOperatorStateType.VoteEnd:
                    return ZeroVoteEnd;
                case ZeroOperatorStateType.Error:
                    return ZeroCommandError;
                case ZeroOperatorStateType.Failed:
                    return ZeroCommandFailed;
                case ZeroOperatorStateType.NotFind:
                    return ZeroCommandNoFind;
                case ZeroOperatorStateType.NotSupport:
                    return ZeroCommandNoSupport;
                case ZeroOperatorStateType.Invalid:
                    return ZeroCommandInvalid;
                case ZeroOperatorStateType.TimeOut:
                    return ZeroCommandTimeout;
                case ZeroOperatorStateType.NetError:
                    return ZeroCommandNetError;
                case ZeroOperatorStateType.NoWorker:
                    return ZeroCommandNotWorker;
                case ZeroOperatorStateType.CommandArgumentError:
                    return ZeroCommandArgError;
                case ZeroOperatorStateType.InstallArgumentError:
                    return ZeroCommandInstallArgError;
                case ZeroOperatorStateType.LocalRecvError:
                    return "-failes. can't recv local data";
                case ZeroOperatorStateType.LocalSendError:
                    return "-failes. can't send local data";
                case ZeroOperatorStateType.LocalException:
                    return "-failes. local throw exception";
                case ZeroOperatorStateType.None:
                    return "*unknow";
                case ZeroOperatorStateType.PlanArgumentError:
                    return "-invalid. plan argument error, must have plan frame"; 
                case ZeroOperatorStateType.RemoteSendError:
                    return "-failes. remote station or ZeroCenter send error";
                case ZeroOperatorStateType.RemoteRecvError:
                    return "-failes. remote station or ZeroCenter recv error";
                case ZeroOperatorStateType.LocalNoReady:
                    return "-failes. ZeroApplication no ready.";
                case ZeroOperatorStateType.LocalZmqError:
                    return "-failes. ZeroMQ  error.";
                default:
                    return ZeroCommandFailed;
            }
        }
    }

}