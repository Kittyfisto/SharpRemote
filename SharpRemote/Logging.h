#pragma once

void Log(const std::wostringstream& message);
bool IsLogEnabled();
bool EnableLogging(const wchar_t* filePath);

#define LOG1(arg1) { if (IsLogEnabled()) { std::wostringstream buffer; buffer << arg1; Log(buffer); } }
#define LOG2(arg1, arg2) { if (IsLogEnabled()) { std::wostringstream buffer; buffer << arg1; buffer << arg2; Log(buffer); } }
#define LOG3(arg1, arg2, arg3) { if (IsLogEnabled()) { std::wostringstream buffer; buffer << arg1; buffer << arg2; buffer << arg3; Log(buffer); } }
#define LOG4(arg1, arg2, arg3, arg4) { if (IsLogEnabled()) { std::wostringstream buffer; buffer << arg1; buffer << arg2; buffer << arg3; buffer << arg4; Log(buffer); } }
#define LOG5(arg1, arg2, arg3, arg4, arg5) { if (IsLogEnabled()) { std::wostringstream buffer; buffer << arg1; buffer << arg2; buffer << arg3; buffer << arg4; buffer << arg5; Log(buffer); } }
#define LOG6(arg1, arg2, arg3, arg4, arg5, arg6) { if (IsLogEnabled()) { std::wostringstream buffer; buffer << arg1; buffer << arg2; buffer << arg3; buffer << arg4; buffer << arg5; buffer << arg6; Log(buffer); } }
#define LOG7(arg1, arg2, arg3, arg4, arg5, arg6, arg7) { if (IsLogEnabled()) { std::wostringstream buffer; buffer << arg1; buffer << arg2; buffer << arg3; buffer << arg4; buffer << arg5; buffer << arg6; buffer << arg7; Log(buffer); } }
#define LOG8(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) { if (IsLogEnabled()) { std::wostringstream buffer; buffer << arg1; buffer << arg2; buffer << arg3; buffer << arg4; buffer << arg5; buffer << arg6; buffer << arg7; buffer << arg8; Log(buffer); } }
#define LOG9(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9) { if (IsLogEnabled()) { std::wostringstream buffer; buffer << arg1; buffer << arg2; buffer << arg3; buffer << arg4; buffer << arg5; buffer << arg6; buffer << arg7; buffer << arg8; buffer << arg9; Log(buffer); } }
