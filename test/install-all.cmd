@echo off
for %%i in (%0) do pushd %%~pi
rd /s /q lib
md lib
..\bin\Debug\NPackage.exe fsinstall /repository=..\web\packages.js log4net mono.cecil mono.options nhibernate nunit sharpziplib
popd