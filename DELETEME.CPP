#include <stdio.h>
#include <stdlib.h>
#include <windows.h>
#include <Psapi.h>

HANDLE get_handle()
{
    
    // Get window
    HWND process_window = FindWindowA(NULL, "Spelunky 2");

    if (process_window == NULL)
    {
        printf("Could not find process window\n");
        exit(-1);
    }

    DWORD process_id;

    // Get process id
    GetWindowThreadProcessId(process_window, &process_id);

    if (process_id == 0)
    {
        printf("Could not find process id\n");
        exit(-1);
    }

    // Read value
    HANDLE process_handle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, process_id);

    if (process_handle == NULL)
    {
        printf("Could not open process\n");
        exit(-1);
    }

    return process_handle;
}


long long get_base_address(const HANDLE hProcess)
{
    HMODULE lphModule[1024];
    DWORD lpcbNeeded(0);

    if (!EnumProcessModules(hProcess, lphModule, sizeof(lphModule), &lpcbNeeded))
    {
        printf("Could not read modules\n");
        exit(-1);
    }

    TCHAR szModName[MAX_PATH];
    if (!GetModuleFileNameEx(hProcess, lphModule[0], szModName, sizeof(szModName) / sizeof(TCHAR)))
    {
        printf("Could not get module info\n");
        exit(-1);
    }

    // First module in list is the executable
    return (long long)lphModule[0];
}



int get_deaths(const HANDLE hProcess, const long long base_address)
{
    long long deaths_address = base_address + 0x22DA8904;

    int deaths;

    // Read value
    ReadProcessMemory(hProcess, (void *)deaths_address, &deaths, sizeof(deaths), NULL);

    return deaths;

}


int main()
{
    HANDLE hProcess = get_handle();
    long long base_address = get_base_address(hProcess);
    
    printf("Base address: %llx\n", base_address);

    printf("Deaths address: %llx\n", base_address + 0x22DA8904);

    int deaths = get_deaths(hProcess, base_address);

    printf("Deaths: %d\n", deaths);

    return 0;
}