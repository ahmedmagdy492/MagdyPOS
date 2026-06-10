#include <Windows.h>
#include <timeapi.h>
#include <fileapi.h>
#include <errhandlingapi.h>

#include <cstdio>
#include <string>
#include <sstream>

#pragma comment(lib, "winmm.lib")

int main(int argc, char* argv[]) {

	if (argc != 2) {
		printf("invalid file name\n");
		return -1;
	}

	DWORD curTime = timeGetTime();
	char newFileName[36];
	memset(newFileName, '\0', 36);
	sprintf_s(newFileName, "%u.db", curTime);

	CopyFileA(argv[1], newFileName, FALSE);

	DWORD drives = GetLogicalDrives();
	char removeableLetter[3] = { '\0' };

	for (int i = 0; i < 26; ++i) {
		if (drives & (1 << i)) {
			char letter[3] = { '\0' };
			letter[0] = 'A' + i;
			letter[1] = ':';
			if (GetDriveTypeA(letter) == DRIVE_REMOVABLE) {
				removeableLetter[0] = 'A' + i;
				removeableLetter[1] = ':';
			}
		}
	}

	if (std::string(removeableLetter) != "") {
		std::stringstream fullCopyPath = std::stringstream(removeableLetter);
		fullCopyPath << removeableLetter;
		fullCopyPath << "\\";
		fullCopyPath << newFileName;
		if (!CopyFileA(newFileName, fullCopyPath.str().c_str(), FALSE))
			return GetLastError();
		return 0;
	}

	return -1;
}