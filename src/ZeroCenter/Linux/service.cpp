// stdafx.cpp : 只包括标准包含文件的源文件
// StreamTest.pch 将作为预编译头
// stdafx.obj 将包含预编译类型信息

#include "stdafx.h"
#include "service.h"
#include <netdb.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <arpa/inet.h>
#include <stdio.h>
#include <signal.h>
#include <time.h>
#include <execinfo.h>
#include <string>
#include "net/station_warehouse.h"
#include "net/station_dispatcher.h"
#include "net/plan_dispatcher.h"


namespace agebull
{
	namespace zmq_net
	{
		ZMQ_HANDLE net_context;
		volatile NET_STATE net_state = NET_STATE_NONE;
		//应该启动的线程数量
		volatile int zero_thread_count = 0;
		//当前启动了多少命令线程
		volatile int zero_thread_run = 0;
		//当前多少线程未正常启动
		volatile int zero_thread_bad = 0;
		/**
		* \brief 任务信号量
		*/
		boost::interprocess::interprocess_semaphore task_semaphore(0);
		boost::interprocess::interprocess_semaphore close_semaphore(0);
		boost::mutex task_mutex;


		/**
		* \brief 主线程等待信号量
		*/
		boost::interprocess::interprocess_semaphore rpc_service::wait_semaphore(0);

		/**
		* \brief 初始化
		*/
		bool rpc_service::initialize()
		{
			//系统信号发生回调绑定
			for (int sig = SIGHUP; sig < SIGSYS; sig++)
				signal(sig, on_sig);
			//ØMQ版本号
			int major, minor, patch;
			zmq_version(&major, &minor, &patch);
			// 初始化配置
			char buf[512];
			acl::string curpath = getcwd(buf, 512);
			var list = curpath.split("/");
			list.pop_back();
			json_config::root_path = "/";
			for (var& word : list)
			{
				json_config::root_path.append(word);
				json_config::root_path.append("/");
			}
			json_config::init();
			// 初始化日志
			var log = log_init();
			log_msg3("ØMQ version:%d.%d.%d", major, minor, patch);
			log_msg3("folder exec:%s\n    root:%s\n    log:%s", curpath.c_str(), json_config::root_path.c_str(), log.c_str());
			//本机IP信息
			acl::string host;
			vector<acl::string> ips;
			get_local_ips(host, ips);
			acl::string ip_info;
			ip_info.format_append("host:%s ips:", host.c_str());
			bool first = true;
			for (var& ip : ips)
			{
				if (first)
					first = false;
				else
					ip_info.append(",");
				ip_info.append(ip);
			}
			log_msg(ip_info);
			//REDIS环境检查
			if (!ping_redis())
			{
				log_error2("redis failed!\n   addr:%s default db:%d", trans_redis::redis_ip(), json_config::redis_defdb);
				return false;
			}
			log_msg2("redis addr:%s default db:%d", trans_redis::redis_ip(), json_config::redis_defdb);
			//站点仓库管理初始化
			return station_warehouse::initialize();
		}

		void rpc_service::start()
		{
			config_zero_center();
			start_zero_center();

			log_msg("zero center in service");
		}

		//等待结束
		void rpc_service::wait_zero()
		{
			wait_semaphore.wait();
		}

		void rpc_service::stop()
		{
			close_net_command();
			acl::log::close();
		}

		//ZMQ上下文对象
		ZMQ_HANDLE get_zmq_context()
		{
			return net_context;
		}

		//线程计数清零
		void reset_command_thread(int count)
		{
			boost::lock_guard<boost::mutex> guard(task_mutex);
			zero_thread_bad = 0;
			zero_thread_run = 0;
			zero_thread_count = count;
		}
		void check_semaphore_start()
		{
			if (zero_thread_run + zero_thread_bad <= 2)
				task_semaphore.post();
			else
				if ((zero_thread_run + zero_thread_bad) == zero_thread_count)
					task_semaphore.post();
		}
		//登记线程开始
		void set_command_thread_run(const char* name)
		{
			boost::lock_guard<boost::mutex> guard(task_mutex);
			zero_thread_run++;
			log_msg2("[%s] zero thread join(%d)", name, zero_thread_run);
			check_semaphore_start();
		}
		//登记线程失败
		void set_command_thread_bad(const char* name)
		{
			boost::lock_guard<boost::mutex> guard(task_mutex);
			zero_thread_bad++;
			log_msg2("[%s] zero thread bad(%d)", name, zero_thread_bad);
			check_semaphore_start();
		}
		//登记线程关闭
		void set_command_thread_end(const char* name)
		{
			boost::lock_guard<boost::mutex> guard(task_mutex);
			zero_thread_run--;
			log_msg2("[%s] zero thread left(%d)", name, zero_thread_run);
			if (zero_thread_run <= 1)
				task_semaphore.post();
		}
		//运行状态
		NET_STATE get_net_state()
		{
			return net_state;
		}

		//初始化网络命令环境
		int config_zero_center()
		{
			log_msg("[zero_center]=>initiate...");
			net_state = NET_STATE_NONE;
			net_context = zmq_ctx_new();
			assert(net_context != nullptr);
			if (json_config::MAX_SOCKETS >= 0)
				zmq_ctx_set(net_context, ZMQ_MAX_SOCKETS, json_config::MAX_SOCKETS);
			if (json_config::IO_THREADS >= 0)
				zmq_ctx_set(net_context, ZMQ_IO_THREADS, json_config::IO_THREADS);
			if (json_config::MAX_MSGSZ >= 0)
				zmq_ctx_set(net_context, ZMQ_MAX_MSGSZ, json_config::MAX_MSGSZ);

			log_msg("[zero_center]=>initiated");
			return net_state;
		}
		//启动网络命令环境
		int start_zero_center()
		{
			//boost::thread thread_xxx(boost::bind(socket_ex::zmq_monitor, nullptr));

			net_state = NET_STATE_RUNING;
			reset_command_thread(static_cast<int>(station_warehouse::get_station_count()));
			log_msg("[zero_center]=>start system dispatcher ...");
			station_warehouse::foreach_configs([](shared_ptr<zero_config>& cfg)
			{
				if (cfg->station_type_ == STATION_TYPE_DISPATCHER)
					log_msg(cfg->to_json(2));
			});
			station_dispatcher::run();
			task_semaphore.wait();
			if (zero_thread_bad == 1)
			{
				log_msg("[zero_center]=>system dispatcher failed ...");
				return	net_state = NET_STATE_FAILED;
			}
			log_msg("[zero_center]=>start plan dispatcher ...");
			station_warehouse::foreach_configs([](shared_ptr<zero_config>& cfg)
			{
				if (cfg->station_type_ > STATION_TYPE_SPECIAL)
					log_msg(cfg->to_json(2));
			});
			plan_dispatcher::run();
			task_semaphore.wait();
			if (zero_thread_bad == 1)
			{
				log_msg("[zero_center]=>plan dispatcher failed ...");
				return	net_state = NET_STATE_FAILED;
			}
			log_msg("[zero_center]=>start business stations...");
			station_warehouse::foreach_configs([](shared_ptr<zero_config>& cfg)
			{
				if (cfg->station_type_ > STATION_TYPE_DISPATCHER && cfg->station_type_ <= STATION_TYPE_SPECIAL)
				{
					log_msg(cfg->to_json(2));
				}
			});
			station_warehouse::restore();
			task_semaphore.wait();
			log_msg("[zero_center]=>all stations in service");
			for (int i = 0; i < 10; i++)
			{
				zero_event_sync("*", zero_net_event::event_system_start, ">>Wecome ZeroNet,luck every day!<<");
				thread_sleep(50);
			}
			log_msg("[zero_center]=>success");
			return net_state;
		}
		void wait_close()
		{
			task_semaphore.wait();
		}
		//关闭网络命令环境
		void close_net_command()
		{
			if (net_state != NET_STATE_RUNING)
				return;
			log_msg("[zero_center]=>closing...");
			for (int i = 0; i < 5; i++)
			{
				zero_event_sync("*", zero_net_event::event_system_closing, "*");
				thread_sleep(40);
			}
			net_state = NET_STATE_CLOSING;
			task_semaphore.wait();
			net_state = NET_STATE_CLOSED;
			zero_event_sync("*", zero_net_event::event_system_stop, ">>ZeroNet close,see you late!<<");
			close_semaphore.post();
			task_semaphore.wait();
			net_state = NET_STATE_DISTORY;
			zmq_ctx_shutdown(net_context);
			zmq_ctx_term(net_context);

			net_context = nullptr;

			log_msg("[zero_center]=>closed");
		}
	}
}
