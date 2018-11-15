#include "stdafx.h"
#include "Logging.h"

using namespace std;

wofstream logfile;
DWORD pid = GetCurrentProcessId();

bool IsLogEnabled()
{
	//return true;
	return false;
}

void Log(const wchar_t* message)
{
	if (!logfile.is_open())
	{
		logfile.open("C:\\Postmortem.log",
			ios::out | ios::app);

		logfile << "Starting new session" << endl;
	}

	SYSTEMTIME time;
	GetLocalTime(&time);

	logfile << pid << " "
		<< time.wYear << "-" << time.wMonth << "-" << time.wDay
		<< " " << time.wHour << ":" << time.wMinute << ":" << time.wSecond << "." << time.wMilliseconds
		<< " " << message << endl;
	logfile.flush();
}

void Log(const wostringstream& message)
{
	Log(message.str().c_str());
}
