#pragma once
#ifndef AGEBULL_CONFIG_H
#include "../stdinc.h"
namespace agebull
{
	class json_config
	{
		/**
		 * \brief 全局配置
		 */
		static std::map<std::string, std::string> global_cfg_;

	public:
		static int base_tcp_port;
		static int plan_exec_timeout;
		static bool use_ipc_protocol;
		static char redis_addr[512];
		static int redis_defdb;
		static int worker_sound_ivl;
		static int IMMEDIATE;
		static int LINGER;
		static int RCVHWM;
		static int RCVBUF;
		static int RCVTIMEO;
		static int SNDHWM;
		static int SNDBUF;
		static int SNDTIMEO;
		static int BACKLOG;
		static int MAX_SOCKETS;
		static int IO_THREADS;
		static int MAX_MSGSZ;
		/**
		* \brief 全局配置初始化
		*/
		static void init();

		/**
		* \brief 系统根目录
		*/
		static acl::string root_path;

		/**
		 * \brief 取全局配置
		 * \param name 名称
		 * \param def 缺省值
		 * \return 值
		 */
		static int get_global_int(const char * name, int def = 0);
		/**
		* \brief 取全局配置
		* \param name 名称
		 * \param def 缺省值
		* \return 值
		*/
		static bool get_global_bool(const char * name, bool def = false);
		/**
		* \brief 取全局配置
		* \param name 名称
		* \return 文本
		*/
		static std::string& get_global_string(const char * name);
	private:
		/**
		* \brief 配置内容
		*/
		std::map<std::string, std::string> value_map_;
		/**
		* \brief 读取配置内容
		*/
		static void read(const char* json, std::map<std::string, std::string>& cfg);
	public:
		/**
		* \brief 构造
		* \param json JSON内容
		*/
		explicit json_config(const char* json);

		/**
		* \brief 取配置
		* \param name 名称
		 * \param def 缺省值
		* \return 数字
		*/
		int number(const char * name, int def = 0);
		/**
		* \brief 取配置
		* \param name 名称
		 * \param def 缺省值
		* \return 布尔
		*/
		bool boolean(const char * name, bool def = false);
		/**
		* \brief 取配置
		* \param name 名称
		* \return 文本
		*/
		std::string& operator[](const char * name);
	};

	/**
	* \brief 大小写敏感的文本匹配，返回匹配的下标（目标的第一个算1，或小等于0表示未找到）
	* \param dests 目标
	* \param src 比较源
	* \return 目标的第一个算0，或小于0表示未找到
	*/
	template<int N>
	int strmatch(const char *src, const char* (&dests)[N]);

	/**
	 * \brief 大小写不敏感的文本匹配，返回匹配的下标（目标的第一个算1，或小等于0表示未找到）
	* \param dests 目标
	* \param src 比较源
	 * \return 目标的第一个算0，或小于0表示未找到
	 */
	template<int N>
	int strmatchi(const char *src, const char* (&dests)[N]);
}
#endif