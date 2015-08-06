#pragma once

#include <string>

std::string convert(const std::wstring& that)
{
	std::string ret;
	ret.resize(that.length()+1);
	std::size_t unused;
	wcstombs_s(&unused, &ret[0], ret.length(), that.c_str(), that.length());
	return ret;
}
