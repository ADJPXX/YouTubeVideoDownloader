# YouTubeVideoDownloader

Aplicação de console em C# para baixar vídeos e playlists do YouTube como MP3 ou MP4. O programa usa o YoutubeExplode para consultar vídeos e streams e o FFmpeg para converter ou combinar os arquivos baixados.

## Funcionalidades

- Baixa um vídeo ou uma playlist em MP3.
- Baixa um vídeo ou uma playlist em MP4, com escolha de resolução.
- Também aceita links de Shorts como links de vídeo.
- Salva os arquivos na pasta `Downloads` do usuário atual.
- Evita sobrescrever arquivos existentes, acrescentando um número ao nome quando necessário.

## Requisitos

- Windows, pois o programa procura especificamente por `ffmpeg.exe`.
- .NET SDK 10.0 para compilar e executar pelo código-fonte.
- FFmpeg disponível em `ffmpeg\ffmpeg.exe` dentro do diretório da aplicação.
- Conexão com a internet.

O projeto usa os pacotes NuGet `YoutubeExplode` 6.6.2 e `YoutubeExplode.Converter` 6.6.0. A conversão executada pelo programa chama o FFmpeg diretamente.

## Preparar o FFmpeg

Antes de iniciar, coloque o executável do FFmpeg no caminho relativo à aplicação:

```text
<diretório-da-aplicação>\ffmpeg\ffmpeg.exe
```

Ao executar em modo Debug com a configuração padrão, por exemplo, o caminho esperado é:

```text
YouTubeVideoDownloader\bin\Debug\net10.0\ffmpeg\ffmpeg.exe
```

O programa encerra ao iniciar se não encontrar esse arquivo. Ele não procura o FFmpeg no `PATH` do sistema.

## Executar pelo código-fonte

Na raiz do repositório, restaure as dependências e inicie o projeto:

```powershell
dotnet restore .\YouTubeVideoDownloader.sln
dotnet run --project .\YouTubeVideoDownloader\YouTubeVideoDownloader.csproj
```

Coloque o FFmpeg no caminho indicado acima antes de iniciar a aplicação.

## Usar o programa

No menu, escolha:

```text
[ 1 ] Baixar MP3 (apenas áudio)
[ 2 ] Baixar MP4 (áudio e vídeo)
[ 0 ] Sair
```

Depois, cole o link de um vídeo, Short ou playlist. Digite `SAIR` no pedido do link para cancelar a operação atual e voltar ao menu.

- **MP3:** baixa o stream de áudio com a maior taxa de bits disponível e converte o arquivo para MP3.
- **MP4:** apresenta as resoluções encontradas, baixa o vídeo e o áudio separadamente e combina os streams. Para playlists, a resolução é escolhida com base no primeiro vídeo; se ela não estiver disponível em outro item, o programa usa a maior resolução disponível para esse item.
- **Playlists:** os itens são processados um de cada vez. Falhas individuais são exibidas e o processamento continua para os demais vídeos.

Os arquivos finais são salvos em:

```text
%USERPROFILE%\Downloads
```

O nome do arquivo é baseado no título do vídeo. Se já existir um arquivo com esse nome, o programa acrescenta sufixos como `(1)` e `(2)`.
## Observação

Use a ferramenta apenas para conteúdo que você tenha autorização para baixar e respeite os direitos autorais e os termos aplicáveis do YouTube.

