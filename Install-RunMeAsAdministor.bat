
@echo off
:NOWINDIR
set /p UserInputPath= Enter the location of your DNN installation (eg. c:\DNN\):
@set var=%UserInputPath%


IF Not %var:~-1%==\ set var=%var%\
 IF NOT EXIST %var% ( 
	echo Path does now exist. Please try again.
 GOTO NOWINDIR
 )
 
 echo %var%
 echo setting up symbolic links...
 
 REM SETUP APP_CODE
 IF NOT EXIST %var%App_Code ( 
	echo Creating App_Code folder
	mkdir %var%App_Code
 )
 cd App_Code
 FOR /d %%G in (*) DO   mklink /J %var%App_Code\%%G %%G
  FOR %%G in (*) DO   mklink /H %var%App_Code\%%G %%G
 
 REM SETUP DESKTOP MODULES
cd ..\DesktopModules\AgapeConnect
IF NOT EXIST %var%DesktopModules\AgapeConnect ( 
	echo Creating DesktopModules\AgapeConnect folder
	mkdir %var%DesktopModules\AgapeConnect
)
FOR /d %%G in (*) DO  mklink /J %var%DesktopModules\AgapeConnect\%%G %%G
cd ..\..
REM SETUP THE MODULE INSTALLERS
copy /Y Install\Module\* %var%Install\Module

powershell -file InstallScripts\replace.ps1 -webConfig %var%web.config


echo complete. Press any key to exit.
pause > nul

 