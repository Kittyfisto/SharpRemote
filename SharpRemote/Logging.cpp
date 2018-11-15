#include "stdafx.h"
#include "Logging.h"

using namespace std;

wofstream logfile;
DWORD pid = GetCurrentProcessId();

bool IsLoggingEnabled()
{
	//return true;
	return false;
}

wstring convert(const string& value)
{
	wstring temp(value.length(), L' ');
	copy(value.begin(), value.end(), temp.begin());
	return temp;
}

wstring convert(const char* value)
{
	const auto length = strlen(value);
	wstring temp(length, L' ');
	copy(value, value + length, temp.begin());
	return temp;
}

const wstring& convert(const wstring& value)
{
	return value;
}

std::wostringstream& operator << (std::wostringstream& buffer, std::string value)
{
	buffer << convert(value);
	return buffer;
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
