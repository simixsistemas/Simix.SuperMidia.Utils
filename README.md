
# Simix.SuperMidia.Utils
 Utilitarios gerais para SuperMidia


 ## Primeira vez aqui?

 - Para baixar o projeto você precisará de um gerenciador de versão, como sugetão você pode utilizar o Github Desktop:

 [Clique para ser redirecionado](https://desktop.github.com/)

 - Após baixar, faça login e selecione "Clone Repository" e busque por Simix/Simix.SuperMidia.Utils
 - Para fazer modificações, selecione o botão "CurrentBranch" clique em "new branch" e forneça um nome que descreve a alteração que você fará.

 > Para próximas modificações, selecione novamente o branch "Master", clique em "Fetch Origin" para atualizar quaisquer alterações feitas na nuvem e apos isso crie um novo branch.

 - Faça as modificações necessárias (adicione/remova arquivos, altere código através de alguma IDE, etc);
 - Volte para o github desktop e verá as modificações na tela à esquerda ("Changes");
 - Faça uma descrição para as modificações, em "Summmary" e clique em `Commit to {seu novo branch}`
 - Clique em Publish Branch e após isso será habilitado um botão "Create pull request", selecione-o
 - Preencha o pull request conforme o template e aguarde a revisão. 🎉


 ## SetupDevice

 Configura um dispositivo, conectado via USB ou Rede, para distribuição em cliente.
 Utiliza um arquivo .Config para executar scripts adb e envia arquivos, para o dispositivo, conforme pastas padrões.
Exemplo de arquivo de configuração (criado automaticamente caso nao exista):

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

- Gerar .exe portátil:
	- Abra o powershell, navegue até as pasta do projeto (onde se encontra o arquivo warp-packer, e execute o comando abaixo:

```Powershell
.\warp-packer --arch windows-x64 --input_dir bin/Release/netcoreapp2.2/win10-x64/publish --exec SetupDevice.exe --output bin/Release/netcoreapp2.2/Portable/SetupDevice_portable.exe
```


## Device Connect

Conecta à dispositivos, android, de forma mais fácil, podendo criar um padrão de nomes para facilitar diferenciação entre os dispositivos.
Informe o Ip do dispositivo e clique em conectar, para o adb criar a ponte de conexão.

> Importante! É necessário habilitar a conexão adb no dispositivo em questão, para isso habilite o menu desenvolvedor e habilite a depuração USB. Para dispositivos à serem conectados pela Wifi, use um direcionamento de porta para :5555 (aqui pode ser utilizada uma aplicação como WifiAdb)

Uma dica é manter o ip fixo dos dispositivos.

<p align="center">
	<kbd>
		<img src="https://user-images.githubusercontent.com/42358163/65694411-34d4ac00-e04c-11e9-9b8a-8d6dc1848f3a.png" alt="image" style="max-width:100%;"/>
	</kbd>
</p>