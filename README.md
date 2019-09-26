
# Simix.SuperMidia.Utils
 Utilitarios gerais para SuperMidia

 ## SetupDevice

 Configura um dispositivo, conectado via USB ou Rede, para distribui��o em cliente.
 Utiliza um arquivo .Config para executar scripts adb e envia arquivos, para o dispositivo, conforme pastas padr�es.
Exemplo de arquivo de configura��o (criado automaticamente caso nao exista):

```Xml
<?xml version="1.0" encoding="utf-8"?>
<SETTINGS>
  <SUPERUSER>
    <UNINSTALL>com.google.android.youtube</UNINSTALL>
    <UNINSTALL>org.xbmc.kodi</UNINSTALL>
    <DISABLE>com.android.soundrecorder</DISABLE>
    <DISABLE>com.android.contacts</DISABLE>
    <DISABLE>com.android.camera2</DISABLE>
    <DISABLE>com.android.calendar</DISABLE>
    <DISABLE>com.android.musicfx</DISABLE>
    <DISABLE>com.android.gallery3d</DISABLE>
    <DISABLE>com.android.calculator2</DISABLE>
    <DISABLE>com.android.email</DISABLE>
    <DISABLE>com.android.music</DISABLE>
    <DISABLE>com.android.quicksearchbox</DISABLE>
    <DISABLE>com.android.deskclock</DISABLE>
    <DISABLE>android.rk.RockVideoPlayer</DISABLE>
    <DISABLE>com.droidlogic.PPPoE</DISABLE>
    <DISABLE>com.android.development</DISABLE>
  </SUPERUSER>
  <EXECUTE>
    <CUSTOM>kill-server</CUSTOM>
    <CUSTOM>start-server</CUSTOM>
  </EXECUTE>
</SETTINGS>
```

- Gerar .exe port�til:
	- Abra o powershell, navegue at� as pasta do projeto (onde se encontra o arquivo warp-packer, e execute o comando abaixo:

```Powershell
.\warp-packer --arch windows-x64 --input_dir bin/Release/netcoreapp2.2/win10-x64/publish --exec SetupDevice.exe --output bin/Release/netcoreapp2.2/Portable/SetupDevice_portable.exe
```


## Device Connect

Conecta � dispositivos, android, de forma mais f�cil, podendo criar um padr�o de nomes para facilitar diferencia��o entre os dispositivos.
Informe o Ip do dispositivo e clique em conectar, para o adb criar a ponte de conex�o.

> Importante! � necess�rio habilitar a conex�o adb no dispositivo em quest�o, para isso habilite o menu desenvolvedor e habilite a depura��o USB. Para dispositivos � serem conectados pela Wifi, use um direcionamento de porta para :5555 (aqui pode ser utilizada uma aplica��o como WifiAdb)

Uma dica � manter o ip fixo dos dispositivos.

<p align="center">
	<kbd>
		<img src="https://user-images.githubusercontent.com/42358163/65694411-34d4ac00-e04c-11e9-9b8a-8d6dc1848f3a.png" alt="image" style="max-width:100%;"/>
	</kbd>
</p>