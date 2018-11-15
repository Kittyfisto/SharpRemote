#include "stdafx.h"
#include "Logging.h"

using namespace std;

wofstream logfile;
bool isLogEnabled = false;

bool IsLogEnabled()
{
	return isLogEnabled;
}

void Log(const wchar_t* message)
{
	SYSTEMTIME time;
	GetLocalTime(&time);

	logfile
		<< time.wYear << "-"
		<< setfill(L'0') << setw(2) << time.wMonth << "-"
		<< setfill(L'0') << setw(2) << time.wDay << " "
		<< setfill(L'0') << setw(2) << time.wHour << ":"
		<< setfill(L'0') << setw(2) << time.wMinute << ":"
		<< setfill(L'0') << setw(2) << time.wSecond << "."
		<< setfill(L'0') << setw(3) << time.wMilliseconds << " "
		<< message << endl;
	logfile.flush();
}

void Log(const wostringstream& message)
{
	Log(message.str().c_str());
}

bool EnableLogging(const wchar_t* filePath)
{
	if (isLogEnabled)
		return false;

	if (!logfile.is_open())
	{
		logfile.open(filePath,
			ios::out | ios::trunc);

		if (logfile.fail())
			return false;

		Log(L"Logging enabled");
	}

	isLogEnabled = true;
	return true;
}
