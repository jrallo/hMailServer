[code]
// GLOBAL
var
  g_pageAccessKey: TInputQueryWizardPage;
  g_szAdminPassword: String;
  
  // BEGIN .NET INSTALLER
  iePath, mdacPath, jetPath, dotnet20Path, msi31Path, msi20Path: string;
  //iePath, dotnet20Path, msi31Path, msi20Path: string;
  downloadNeeded: boolean;
  neededDependenciesDownloadMemo: string;
  neededDependenciesInstallMemo: string;
  neededDependenciesDownloadMsg: string;
  // END .NET INSTALLER

// The NT-service specific parts of the scrit below is taken
// from the innosetup extension knowledgebase.
// Author: Silvio Iaccarino silvio.iaccarino(at)de.adp.com 
// Article created: 6 November 2002
// Article updated: 6 November 2002
// http://www13.brinkster.com/vincenzog/isxart.asp?idart=31

type
	SERVICE_STATUS = record
    	dwServiceType				: cardinal;
    	dwCurrentState				: cardinal;
    	dwControlsAccepted			: cardinal;
    	dwWin32ExitCode				: cardinal;
    	dwServiceSpecificExitCode	: cardinal;
    	dwCheckPoint				: cardinal;
    	dwWaitHint					: cardinal;
	end;
	HANDLE = cardinal;
	
const
	SERVICE_QUERY_CONFIG		= $1;
	SERVICE_CHANGE_CONFIG		= $2;
	SERVICE_QUERY_STATUS		= $4;
	SERVICE_START				= $10;
	SERVICE_STOP				= $20;
	SERVICE_ALL_ACCESS			= $f01ff;
	SC_MANAGER_ALL_ACCESS		= $f003f;
	SERVICE_WIN32_OWN_PROCESS	= $10;
	SERVICE_WIN32_SHARE_PROCESS	= $20;
	SERVICE_WIN32				= $30;
	SERVICE_INTERACTIVE_PROCESS = $100;
	SERVICE_BOOT_START          = $0;
	SERVICE_SYSTEM_START        = $1;
	SERVICE_AUTO_START          = $2;
	SERVICE_DEMAND_START        = $3;
	SERVICE_DISABLED            = $4;
	SERVICE_DELETE              = $10000;
	SERVICE_CONTROL_STOP		= $1;
	SERVICE_CONTROL_PAUSE		= $2;
	SERVICE_CONTROL_CONTINUE	= $3;
	SERVICE_CONTROL_INTERROGATE = $4;
	SERVICE_STOPPED				= $1;
	SERVICE_START_PENDING       = $2;
	SERVICE_STOP_PENDING        = $3;
	SERVICE_RUNNING             = $4;
	SERVICE_CONTINUE_PENDING    = $5;
	SERVICE_PAUSE_PENDING       = $6;
	SERVICE_PAUSED              = $7;
  // BEGIN .NET INSTALLER	
  msi20URL = 'http://download.microsoft.com/download/WindowsInstaller/Install/2.0/W9XMe/EN-US/InstMsiA.exe';
  msi31URL = 'http://download.microsoft.com/download/1/4/7/147ded26-931c-4daf-9095-ec7baf996f46/WindowsInstaller-KB893803-v2-x86.exe';
  mdacURL = 'http://download.microsoft.com/download/c/d/f/cdfd58f1-3973-4c51-8851-49ae3777586f/MDAC_TYP.EXE';
  jetURL = 'http://download.microsoft.com/download/4/3/9/4393c9ac-e69e-458d-9f6d-2fe191c51469/Jet40SP8_9xNT.exe';
  ieURL = 'http://download.microsoft.com/download/ie6sp1/finrel/6_sp1/W98NT42KMeXP/EN-US/ie6setup.exe';
  dotnet20URL = 'http://download.microsoft.com/download/5/6/7/567758a3-759e-473e-bf8f-52154438565a/dotnetfx.exe';
  // END .NET INSTALLER	

function ControlService(hService :HANDLE; dwControl :cardinal;var ServiceStatus :SERVICE_STATUS) : boolean;
external 'ControlService@advapi32.dll stdcall';		

function CloseServiceHandle(hSCObject :HANDLE): boolean;
external 'CloseServiceHandle@advapi32.dll stdcall';

function OpenService(hSCManager :HANDLE;lpServiceName: string; dwDesiredAccess :cardinal): HANDLE;
external 'OpenServiceA@advapi32.dll stdcall';

function OpenSCManager(lpMachineName, lpDatabaseName: string; dwDesiredAccess :cardinal): HANDLE;
external 'OpenSCManagerA@advapi32.dll stdcall';

function QueryServiceStatus(hService :HANDLE;var ServiceStatus :SERVICE_STATUS) : boolean;
external 'QueryServiceStatus@advapi32.dll stdcall';

function CheckPorts(): Integer;
external 'CheckPorts@files:ISC.DLL stdcall';


function isxdl_Download(hWnd: Integer; URL, Filename: PChar): Integer;
external 'isxdl_Download@files:isxdl.dll stdcall';

procedure isxdl_AddFile(URL, Filename: PChar);
external 'isxdl_AddFile@files:isxdl.dll stdcall';

procedure isxdl_AddFileSize(URL, Filename: PChar; Size: Cardinal);
external 'isxdl_AddFileSize@files:isxdl.dll stdcall';

function isxdl_DownloadFiles(hWnd: Integer): Integer;
external 'isxdl_DownloadFiles@files:isxdl.dll stdcall';

procedure isxdl_ClearFiles;
external 'isxdl_ClearFiles@files:isxdl.dll stdcall';

function isxdl_IsConnected: Integer;
external 'isxdl_IsConnected@files:isxdl.dll stdcall';

function isxdl_SetOption(Option, Value: PChar): Integer;
external 'isxdl_SetOption@files:isxdl.dll stdcall';

function isxdl_GetFileName(URL: PChar): PChar;
external 'isxdl_GetFileName@files:isxdl.dll stdcall';

// get Windows Installer version
procedure DecodeVersion(const Version: cardinal; var a, b : word);
begin
  a := word(Version shr 16);
  b := word(Version and not $ffff0000);
end;

function IsMinMSIAvailable(HV:Integer; NV:Integer ): boolean;
var  Version,  dummy     : cardinal;
     MsiHiVer,  MsiLoVer  : word;

begin
    Result:=(FileExists(ExpandConstant('{sys}\msi.dll'))) and
        (GetVersionNumbers(ExpandConstant('{sys}\msi.dll'), Version, dummy));
    DecodeVersion(Version, MsiHiVer, MsiLoVer);
    Result:= (Result) and (MsiHiVer >= HV) and (MsiLoVer >= NV);
end;

function OpenServiceManager() : HANDLE;
begin
	if UsingWinNT() = true then begin
		Result := OpenSCManager('','ServicesActive',SC_MANAGER_ALL_ACCESS);
		if Result = 0 then
			MsgBox('the servicemanager is not available', mbError, MB_OK)
	end
	else begin
			MsgBox('only nt based systems support services', mbError, MB_OK)
			Result := 0;
	end
end;

function IsServiceInstalled(ServiceName: string) : boolean;
var
	hSCM	: HANDLE;
	hService: HANDLE;
begin
	hSCM := OpenServiceManager();
	Result := false;
	if hSCM <> 0 then begin
		hService := OpenService(hSCM,ServiceName,SERVICE_QUERY_CONFIG);
        if hService <> 0 then begin
            Result := true;
            CloseServiceHandle(hService)
		end;
        CloseServiceHandle(hSCM)
	end
end;

function StopService(ServiceName: string) : boolean;
var
	hSCM	: HANDLE;
	hService: HANDLE;
	Status	: SERVICE_STATUS;
begin
	hSCM := OpenServiceManager();
	Result := false;
	if hSCM <> 0 then begin
		hService := OpenService(hSCM,ServiceName,SERVICE_STOP);
        if hService <> 0 then begin
        	Result := ControlService(hService,SERVICE_CONTROL_STOP,Status);
            CloseServiceHandle(hService)
		end;
        CloseServiceHandle(hSCM)
	end;
end;

function IsServiceRunning(ServiceName: string) : boolean;
var
	hSCM	: HANDLE;
	hService: HANDLE;
	Status	: SERVICE_STATUS;
begin
	hSCM := OpenServiceManager();
	Result := false;
	if hSCM <> 0 then begin
		hService := OpenService(hSCM,ServiceName,SERVICE_QUERY_STATUS);
    	if hService <> 0 then begin
			if QueryServiceStatus(hService,Status) then begin
				Result :=(Status.dwCurrentState = SERVICE_RUNNING)
        	end;
            CloseServiceHandle(hService)
		    end;
        CloseServiceHandle(hSCM)
	end
end;

function IsServiceStopped(ServiceName: string) : boolean;
var
	hSCM	: HANDLE;
	hService: HANDLE;
	Status	: SERVICE_STATUS;
begin
	hSCM := OpenServiceManager();
	Result := false;
	if hSCM <> 0 then begin
		hService := OpenService(hSCM,ServiceName,SERVICE_QUERY_STATUS);
    	if hService <> 0 then begin
			if QueryServiceStatus(hService,Status) then begin
				Result :=(Status.dwCurrentState = SERVICE_STOPPED)
        	end;
            CloseServiceHandle(hService)
		    end;
        CloseServiceHandle(hSCM)
	end
end;

function GetInifile() : String;
var
   szInifile : String;
begin

   // Check if the file exists in the selected installation directory.
   szInifile := ExpandConstant('{app}\Bin\hMailServer.ini');
   
   if (FileExists(szInifile) = False) then
   begin

      if RegQueryStringValue(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\hMailServer_is1','InstallLocation', szInifile) then
      begin
         szInifile := szInifile + 'Bin\hMailServer.ini';
      end;

      if (FileExists(szInifile) = False) then
      begin
        // File doesn't exist in the old installation directory.
        szInifile := ExpandConstant('{win}\hMailServer.ini');

        if (FileExists(szInifile) = False) then
        begin
           szInifile := '';
        end;
      end;

   end;

   Result := szInifile;

end;

function GetCurrentDatabaseType() : String;
var
   szInifile : String;
   szDatabaseType : String;
begin

   // Locate the ini file.
   szInifile := GetInifile();

   // Read the database settings...
   szDatabaseType := GetIniString('Database', 'Type', '', szIniFile);
   Result := Lowercase(szDatabaseType);
end;


function GetAdministratorPassword() : string;
	var szIniFile : String;
	var sKey : String;
begin
	szIniFile := GetInifile();
	
	sKey := GetIniString('Security', 'AdministratorPassword', '', szIniFile);
	
	Result := sKey;
end;


function ShouldSkipPage(PageID: Integer): Boolean;
begin
   Result := false;
   
   if (WizardSilent() = false) then
   begin
	   // Check if we should skip the password dialog.
	   if (PageID = g_pageAccessKey.ID) then
	   begin
	
	      // If server installation has not been selected, we should skip it,
	      // since the password is for the server...
	      if (IsComponentSelected('server') = false) then
	      begin
	         Result := true;
	      end;
	
	      // If a password has already been specified, we should skip it as well,
	      // It's not possible to change an existing password.
	   	if Length(GetAdministratorPassword()) > 0 then
	   	begin
		   	// Password already specified - skip page.
		   	  Result:= true;
		   end;
	   end;
	     end;

end;

procedure moreInfoLink_Click(Sender: TObject);
var
  ErrorCode: Integer;
begin
  ShellExec('', 'http://www.hmailserver.com/documentation/?page=choosing_database_engine', '', '', SW_SHOW, ewNoWait, ErrorCode);

end;

procedure CreateWizardPages();
var
   moreInfoLink : TLabel;
   moreInfoFont : TFont;
   
begin

 	 // Create key page
   g_pageAccessKey := CreateInputQueryPage(wpSelectTasks, 'hMailServer Security', 'Specify main password','The installation program will now create a hMailServer user with administration rights. Please enter a password below. You will need this password to be able to manage your hMailServer installation, so please remember it.');	
  
   g_pageAccessKey.Add('Password:', True);
   g_pageAccessKey.Add('Confirm password:', True);
   
end;

procedure InitializeWizard();
begin
   if (WizardSilent() = false) then
   begin
      CreateWizardPages();
   end;
end;

function InitializeSetup(): Boolean; 
	var 
		sMessage : String;
    SoftwareVersion: string;
    WindowsVersion: TWindowsVersion;		
begin
	Result := true;

	if (FindWindowByWindowName('hMailServer Administrator') > 0) then
	begin
		MsgBox('hMailServer Administrator is started. You must close down this application before starting the installation.',mbInformation, MB_OK);	
		Result := false;
		Exit;
	end;
	
	if (Result = true) then
	begin
		if (FindWindowByWindowName('hMailServer Database Setup') > 0) then
		begin
			MsgBox('hMailServer Database Setup is started. You must close down this application before starting the installation.',mbInformation, MB_OK);	
			Result := false;
      Exit;
		end;	
	end;
	
	if (Result = true) then
	begin
		if (FindWindowByWindowName('hMailServer Database Upgrader') > 0) then
		begin
			MsgBox('hMailServer Database Upgrader is started. You must close down this application before starting the installation.',mbInformation, MB_OK);	
			Result := false;
      Exit;			
		end;	
	end;	
	
	if (Result = true) then
	begin
		if (FindWindowByWindowName('DBSetup') > 0) then
		begin
			MsgBox('hMailServer DBSetup is started. You must close down this application before starting the installation.',mbInformation, MB_OK);	
			Result := false;
      Exit;			
		end;	
	end;	
	

	// Check so that there isn't already a server running
	// on one of hour ports.
	if (Result = true) then
	begin
		if (IsServiceRunning('hMailServer') <> True) And (CheckPorts() < 0) then
		begin
			// The hMailServer isn't running, but someone is blocking the ports.
			//
			sMessage := 'The hMailServer Setup has detected that one or several of the TCP/IP ports 25, 110 and 143 are already in use.' + Chr(13) + Chr(10) + 'This indicates that there already is an email server running on this computer.' + Chr(13) + Chr(10) + 'If you plan to use any of these ports with hMailServer, the already existing server must be stopped.';
			MsgBox(sMessage, mbInformation, MB_OK);	
		end;
	end;	


  GetWindowsVersionEx(WindowsVersion);
  Result := true;

  // Check for Windows 2000 SP3
  if WindowsVersion.NTPlatform and
     (WindowsVersion.Major = 5) and
     (WindowsVersion.Minor = 0) and
     (WindowsVersion.ServicePackMajor < 3) then
  begin
    MsgBox(CustomMessage('Win2000Sp3Msg'), mbError, MB_OK);
    Result := false;
    exit;
  end;

  // Check for Windows XP SP2
  if WindowsVersion.NTPlatform and
     (WindowsVersion.Major = 5) and
     (WindowsVersion.Minor = 1) and
     (WindowsVersion.ServicePackMajor < 2) then
  begin
    MsgBox(CustomMessage('WinXPSp2Msg'), mbError, MB_OK);
    Result := false;
    exit;
  end;

  // Check for IIS installation
  //if not RegKeyExists(HKLM, 'SYSTEM\CurrentControlSet\Services\W3SVC\Security') then begin
  //  MsgBox(CustomMessage('IISMsg'), mbError, MB_OK);
  //  Result := false;
  //  exit;
  //end;

  // Check for required Windows Installer 2.0 on Windows 98 and ME
  if (not WindowsVersion.NTPlatform) and
     (WindowsVersion.Major >= 4) and
     (WindowsVersion.Minor >= 1) and
     (not IsMinMSIAvailable(2,0)) then
  begin
    neededDependenciesInstallMemo := neededDependenciesInstallMemo + '      ' + CustomMessage('MSI20Title') + #13;
    msi20Path := ExpandConstant('{src}') + '\' + CustomMessage('DependenciesDir') + '\instmsia.exe';
    if not FileExists(msi20Path) then begin
      msi20Path := ExpandConstant('{tmp}\msi20.exe');
      if not FileExists(msi20Path) then begin
        neededDependenciesDownloadMemo := neededDependenciesDownloadMemo + '      ' + CustomMessage('MSI20Title') + #13;
        neededDependenciesDownloadMsg := neededDependenciesDownloadMsg + CustomMessage('MSI20Title') + ' (' + CustomMessage('MSI20DownloadSize') + ')' + #13;
        isxdl_AddFile(msi20URL, msi20Path);
        downloadNeeded := true;
      end;
    end;
    SetIniString('install', 'msi20', msi20Path, ExpandConstant('{tmp}\dep.ini'));
  end;

  // Check for required Windows Installer 3.0 on Windows 2000, XP, Server 2003, Vista or higher
  if WindowsVersion.NTPlatform and
     (WindowsVersion.Major >= 5) and
     (not IsMinMSIAvailable(3,0)) then
  begin
    neededDependenciesInstallMemo := neededDependenciesInstallMemo + '      ' + CustomMessage('MSI31Title') + #13;
    msi31Path := ExpandConstant('{src}') + '\' + CustomMessage('DependenciesDir') + '\WindowsInstaller-KB893803-v2-x86.exe';
    if not FileExists(msi31Path) then begin
      msi31Path := ExpandConstant('{tmp}\msi31.exe');
      if not FileExists(msi31Path) then begin
        neededDependenciesDownloadMemo := neededDependenciesDownloadMemo + '      ' + CustomMessage('MSI31Title') + #13;
        neededDependenciesDownloadMsg := neededDependenciesDownloadMsg + CustomMessage('MSI31Title') + ' (' + CustomMessage('MSI31DownloadSize') + ')' + #13;
        isxdl_AddFile(msi31URL, msi31Path);
        downloadNeeded := true;
      end;
    end;
    SetIniString('install', 'msi31', msi31Path, ExpandConstant('{tmp}\dep.ini'));
  end;

  // Check for required Internet Explorer installation
  // Note that if Internet Explorer 6 is downloaded, the express setup will be downloaded, however it is the same
  // ie6setup.exe that would be available in the ie6 folder. The only difference is that the
  // user will be presented with an option as to where to download Internet Explorer 6 and a progress dialog.
  // Most common components will still be installed automatically.
  SoftwareVersion := '';
  RegQueryStringValue(HKLM, 'Software\Microsoft\Internet Explorer', 'Version', SoftwareVersion);
  if (SoftwareVersion < '5') then begin
    neededDependenciesInstallMemo := neededDependenciesInstallMemo + '      ' + CustomMessage('IE6Title') + #13;
    iePath := ExpandConstant('{src}') + '\' + CustomMessage('DependenciesDir') + '\ie6\ie6setup.exe';
    if not FileExists(iePath) then begin
      iePath := ExpandConstant('{tmp}\ie6setup.exe');
      if not FileExists(iePath) then begin
        neededDependenciesDownloadMemo := neededDependenciesDownloadMemo + '      ' + CustomMessage('IE6Title') + #13;
        neededDependenciesDownloadMsg := neededDependenciesDownloadMsg + CustomMessage('IE6Title') + ' (' + CustomMessage('IE6DownloadSize') + ')' + #13;
        isxdl_AddFile(ieURL, iePath);
        downloadNeeded := true;
      end;
    end;
    SetIniString('install', 'ie', iePath, ExpandConstant('{tmp}\dep.ini'));
  end;
	
  // Check for required MDAC installation
  SoftwareVersion := '';
  RegQueryStringValue(HKLM, 'Software\Microsoft\DataAccess', 'FullInstallVer', SoftwareVersion);
  if (SoftwareVersion < '2.7') then begin
    neededDependenciesInstallMemo := neededDependenciesInstallMemo + '      ' + CustomMessage('MDACTitle') + #13;
    mdacPath := ExpandConstant('{src}') + '\' + CustomMessage('DependenciesDir') + '\MDAC_TYP.EXE';
    if not FileExists(mdacPath) then begin
      mdacPath := ExpandConstant('{tmp}\MDAC_TYP.EXE');
      if not FileExists(mdacPath) then begin
        neededDependenciesDownloadMemo := neededDependenciesDownloadMemo + '      ' + CustomMessage('MDACTitle') + #13;
        neededDependenciesDownloadMsg := neededDependenciesDownloadMsg + CustomMessage('MDACTitle') + ' (' + CustomMessage('MDACDownloadSize') + ')' + #13;
        isxdl_AddFile(mdacURL, mdacPath);
        downloadNeeded := true;
      end;
    end;
    SetIniString('install', 'mdac', mdacPath, ExpandConstant('{tmp}\dep.ini'));
  end;

	// Check for required Jet installation
	if (not RegKeyExists(HKLM, 'Software\Microsoft\Jet\4.0')) then begin
	  neededDependenciesInstallMemo := neededDependenciesInstallMemo + '      ' + CustomMessage('JETTitle') + #13;
	  jetPath := ExpandConstant('{src}') + '\' + CustomMessage('DependenciesDir') + '\Jet40SP8_9xNT.exe';
	  if not FileExists(jetPath) then begin
	    jetPath := ExpandConstant('{tmp}\Jet40SP8_9xNT.exe');
	    if not FileExists(jetPath) then begin
	      neededDependenciesDownloadMemo := neededDependenciesDownloadMemo + '      ' + CustomMessage('JETTitle') + #13;
        neededDependenciesDownloadMsg := neededDependenciesDownloadMsg + CustomMessage('JETTitle') + ' (' + CustomMessage('JETDownloadSize') + ')' + #13;
	      isxdl_AddFile(jetURL, jetPath);
	      downloadNeeded := true;
	    end;
	  end;
	  SetIniString('install', 'jet', jetPath, ExpandConstant('{tmp}\dep.ini'));
	end;

  // Check for required dotnetfx 2.0 installation
  if (not RegKeyExists(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v2.0.50727')) then begin
    neededDependenciesInstallMemo := neededDependenciesInstallMemo + '      ' + CustomMessage('DOTNET20Title') + #13;
    dotnet20Path := ExpandConstant('{src}') + '\' + CustomMessage('DependenciesDir') + '\dotnetfx.exe';
    if not FileExists(dotnet20Path) then begin
      dotnet20Path := ExpandConstant('{tmp}\dotnetfx.exe');
      if not FileExists(dotnet20Path) then begin
        neededDependenciesDownloadMemo := neededDependenciesDownloadMemo + '      ' + CustomMessage('DOTNET20Title') + #13;
        neededDependenciesDownloadMsg := neededDependenciesDownloadMsg + CustomMessage('DOTNET20Title') + ' (' + CustomMessage('DOTNET20DownloadSize') + ')' + #13;
        isxdl_AddFile(dotnet20URL, dotnet20Path);
        downloadNeeded := true;
      end;
    end;
    SetIniString('install', 'dotnet20', dotnet20Path, ExpandConstant('{tmp}\dep.ini'));
  end;



end;



function RegisterTypeLib() : Boolean;
var
  ResultCode: Integer;
begin
   // Register hMaiLlServer service
   if (Exec(ExpandConstant('{app}\Bin\hMailServer.exe'), '/RegisterTypeLib', '',  SW_HIDE, ewWaitUntilTerminated, ResultCode) = False) then
      MsgBox(SysErrorMessage(ResultCode), mbError, MB_OK);

   Result := true;
end;


function DeleteOldFiles() : Boolean;
begin

   DeleteFile(ExpandConstant('{app}\Bin\Copyright.txt'));
   DeleteFile(ExpandConstant('{app}\Bin\License.txt'));
   DeleteFile(ExpandConstant('{app}\Bin\Sourcecode.txt'));

   Result := true;
end;

function RunPostInstallTasks() : Boolean;
   var
      ResultCode: Integer;
      ProgressPage : TOutputProgressWizardPage;
      szParameters: String;
begin
   try

      ProgressPage := CreateOutputProgressPage('Finalizing installation','Please wait while the setup performs post-installation tasks');
      ProgressPage.Show();

      ProgressPage.SetText('Creating the hMailServer service...', '');
      ProgressPage.SetProgress(1,4);

      // Register the hMailServer service
      if (Exec(ExpandConstant('{app}\Bin\hMailServer.exe'), '/Register', '',  SW_HIDE, ewWaitUntilTerminated, ResultCode) = False) then
         MsgBox(SysErrorMessage(ResultCode), mbError, MB_OK);

      ProgressPage.SetText('Initializing hMailServer database...', '');
      ProgressPage.SetProgress(2,4);

      if (WizardSilent() = true) then
      begin
          szParameters:= '/silent';
      end
      
      // Add the password as well, so that the administrator doesn't have to type it in again
      // if he have just entered it. If this is an upgrade, he'll have to enter it again though.
      if (Length(g_szAdminPassword) > 0) then
         szParameters := szParameters + ' password:' + g_szAdminPassword;

      // Start DBSetupQuick. It will either launch DBUpdater.exe to update an existing database
      // or it will launch DBSetup.exe to start a new one.
      if (Exec(ExpandConstant('{app}\Bin\DBSetupQuick.exe'), szParameters, '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode) = False) then
         MsgBox(SysErrorMessage(ResultCode), mbError, MB_OK);

      ProgressPage.SetText('Starting the hMailServer service...', '');
      ProgressPage.SetProgress(3,4);

      // Start hMailServer
      if (Exec(ExpandConstant('{sys}\net.exe'), 'START hMailServer', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) = False) then
         MsgBox(SysErrorMessage(ResultCode), mbError, MB_OK);

      ProgressPage.SetText('Completed', '');
      ProgressPage.SetProgress(4,4);

   finally
     ProgressPage.Hide();
   end;

   Result := true;
   
end;

function MoveIni() : Boolean;
  var sOldFile : String;
  var sNewFile : String;
begin

   CreateDir(ExpandConstant('{app}\Bin'));
   sOldFile := ExpandConstant('{win}\hMailServer.ini');
   sNewFile := ExpandConstant('{app}\Bin\hMailServer.ini');

   // Copy the file from the Windows directory
   // to the Bin directory. hMailServer uses the
   // file located in the Bin directory.
   if (FileCopy(sOldFile, sNewFile, True) = True) then
   begin
      // Rename the old hmailserver.ini
      sNewFile := sOldFile + '.old';
      if (FileCopy(sOldFile, sNewFile, True) = True) then
      begin
        // We've managed to backup hMailServer.ini in the
        // windows directory. Now delete the original.
        DeleteFile(sOldFile);
      end;
   end;
   Result := true;

end;

function CheckIsOldMySQLInstallation(szIniFile: String) : boolean;
var
   szDatabasePort : String;
   szProgramFolder: String;
   szMySQLExecutable : String;
   iFileSize: Integer;
   szMessage : String;
   szDatabase : String;
   szDatabaseHost : String;
   szDatabaseUsername : String;
begin

  szDatabasePort := GetIniString('Database', 'Port', '', szIniFile);
  szDatabase := GetIniString('Database', 'Database', '', szIniFile);
  szDatabaseHost := GetIniString('Database', 'Server', '', szIniFile);
  szDatabaseUsername := GetIniString('Database', 'Username', '', szIniFile);
  szProgramFolder := RemoveBackslash(GetIniString('Directories', 'ProgramFolder', '', szIniFile));

  if (GetCurrentDatabaseType() = 'mysql') and
     (szDatabasePort = '3307') and
     (szDatabase = 'hMailServer') and
     (szDatabaseHost = 'localhost') and
     (szDatabaseUsername = 'root') then begin
     
    // We're using an internal MySQL database.
    szMySQLExecutable := szProgramFolder + '\MySQL\Bin\mysqld-nt.exe';

    // Check the size of MySQL.
    iFileSize := 0;
    if (FileSize(szMySQLExecutable, iFileSize)) then begin
       // MySQL in 4.4.3 is larger than 3500000 bytes.
       if (iFileSize < 3500000) then begin
          // MySQL is too old.
          
          szMessage := 'This version of hMailServer does not include MySQL. hMailServer can still' + #13 +
                       'use MySQL as backend though, assuming it is already installed on the system.' + #13 +
                       '' + #13 +
                       'However, the MySQL version hMailServer is configured to use is'  + #13 +
                       'too old for this version of hMailServer.' + #13 +
                       ''+ #13 +
                       'To solve this issue you must install the latest hMailServer 4 version' + #13 +
                       'before upgrading to hMailServer 5.' + #13
                       ''+ #13 +
                       'The latest hMailServer 4 version will upgrade MySQL to an version' + #13 +
                       'which is compatible with hMailServer 5.'

  		  	MsgBox(szMessage, mbError, MB_OK)
          Result := true;
       end;
    end
    else
    begin
          szMessage := 'hMailServer 5 and later does not include MySQL. hMailServer 5 can still' + #13 +
                       'use MySQL as backend though, assuming it is already installed on the system.' + #13 +
                       '' + #13 +
                       'You have configured hMailServer 4 to use the bundled MySQL installation. However'+ #13 +
                       'hMailServer 4 with MySQL appears to have been uninstalled prior to running this' + #13 +
                       'hMailServer 5 installation. Hence, the MySQL installation hMailServer needs is' + #13 +
                       'no longer available.' + #13 +
                       '' + #13 +
                       'To solve this problem, reinstall the same hMailServer 4 version as before and then' + #13 +
                       'upgrade to version 5 without first uninstalling version 4.' + #13 +
                       '' + #13 +
                       'As an alternative, you can cancel this installation, delete the entire hMailServer ' + #13 +
                       'directory and then run this installation program again. Using this method, your configuration' + #13 +
                       'and email messages will be lost.';
                       
  		  	MsgBox(szMessage, mbError, MB_OK)
          Result := true;
    end;

  end;


end;

function NextButtonClick(CurPage : Integer): boolean;
var
	 sPassword : String;
   hWnd: Integer;
   bInstallNet: boolean;
   szIniFile : String;

begin
	// We default to true.
	Result := true;

  if (CurPage = wpSelectDir) then
  begin
  
    szIniFile := GetIniFile();

    // Check if this folder contains an old MySQL installation, or if
    // the old MySQL installation has been uninstalled.
    if CheckIsOldMySQLInstallation(szIniFile) = true then begin
        Result := false;
    end;
  end
  else if CurPage = wpReady then
	begin
		// Start hMailServer and MySQL, if they are running.
		if IsServiceRunning('hMailServer') = true then
		begin
		 	 StopService('hMailServer');
		   
		   while (IsServiceStopped('hMailServer') = false) do
		   begin
		      Sleep(250);
		   end;
    end;
	   
    hWnd := StrToInt(ExpandConstant('{wizardhwnd}'));

    bInstallNet := false;

    if downloadNeeded then
    begin
       // If we're running in silent mode, just install .NET
       // automatically.
       if WizardSilent() = true then
       begin
          bInstallNet := true;
       end
       else
       begin
          // Ask the user if we should install .NET
          if MsgBox(CustomMessage('DownloadMsg1') + #13 + neededDependenciesDownloadMsg + #13 + CustomMessage('DownloadMsg2'), mbConfirmation, MB_YESNO) = IDYES then
          begin
             bInstallNet := true;
          end;
       end;
       
       if bInstallNet then
       begin
          if isxdl_DownloadFiles(hWnd) = 0 then
          begin
             // Installation of .NET failed.
             Result := false;
          end;
       end
       else
       begin
         // .NET is required but the user has selected not to install it.
         Result := false;
       end;

       // end downloadNeeded
    end;
	end;

	
	if WizardSilent() = false then
	begin
  	if CurPage = g_pageAccessKey.ID then
	 begin
  		// Check that passwords matches.
	 	  if (Length(g_pageAccessKey.Values[0]) < 5) or (g_pageAccessKey.Values[0] <> g_pageAccessKey.Values[1]) then
  	 	begin
	 	  	 MsgBox('The two passwords must match and be at least 5 characters long.', mbError, MB_OK)
  	 		 Result := false;
     	end;
     	
     	g_szAdminPassword := g_pageAccessKey.Values[0];
   	end;
  end;

end;

procedure CurPageChanged(CurPageID: Integer);
begin

end;

function GetHashedPassword(Param: String): String;
begin
  Result := GetMD5OfString(g_szAdminPassword);
end;


procedure CurStepChanged(CurStep: TSetupStep);
var
  szIniFile  : String;
  szPassword : String;
begin
	if CurStep = ssInstall then
	begin
	   // Move hMailServer.ini before files are copied
	   MoveIni();

  	 // Write the administrator password if it has been
	   // specified during this set up. If it has been specified
	   // before, we keep the old one.
	   // szPassword := g_pageAccessKey.Values[0];

	   // 	   if (Length(szPassword) >= 5) then
	   // 	   begin
	   //    	    g_szAdminPassword := szPassword;
	   //         SetIniString('Security', 'AdministratorPassword', GetMD5OfString(szPassword), ExpandConstant('{app}\Bin\hMailServer.ini'));	
	   // 	   end;
	end;
	
	if CurStep = ssPostInstall then
	begin
   // Create a registry key that tell
	  // other apps where we're installed.
	  RegWriteStringValue(HKEY_LOCAL_MACHINE, 'Software\hMailServer', 'InstallLocation', ExpandConstant('{app}'));
   	
	  // Write db location to hMailServer.ini.
	  szIniFile := ExpandConstant('{app}\Bin\hMailServer.ini');

  	// Create the hMailServer database
 	  if (IsComponentSelected('server')) then
	  begin
	    RunPostInstallTasks();
	  end
	 else
	 begin
	   if (IsComponentSelected('admintools')) then
	   begin
	      RegisterTypeLib();
	   end;
	 end;

   DeleteOldFiles();
   
	end;
 
end;




function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
var
  s: string;

begin
  if neededDependenciesDownloadMemo <> '' then s := s + CustomMessage('DependenciesDownloadTitle') + ':' + NewLine + neededDependenciesDownloadMemo + NewLine;
  if neededDependenciesInstallMemo <> '' then s := s + CustomMessage('DependenciesInstallTitle') + ':' + NewLine + neededDependenciesInstallMemo + NewLine;

  s := s + MemoDirInfo + NewLine + NewLine + MemoGroupInfo
  if MemoTasksInfo <> '' then  s := s + NewLine + NewLine + MemoTasksInfo;

  Result := s
end;

