# Apuracoes Bot
Bot para discord utilizado ontem para a visualização em tempo real das apurações das eleições do 1º e 2º turno.<br>Dados fornecidos para todos através do TSE pelo site: <a href="https://resultados.tse.jus.br/">https://resultados.tse.jus.br/</a>

### Como Executar
1. Defina a variável de ambiente `BOT_TOKEN` com a token do seu bot fornecido pelo discord.`
2. Inicie o bot pelo comando `dotnet ApuracoesApp.dll`
3. Use o comando `e!ajuda` para obter ajuda do bot.

### Compilando

1. Clone o repositório.
2. Abra o CMD na pasta do repositório.
3. Execute o comando `dotnet build -c Release`
4. Obtenha os executáveis na pasta `bin\Release\net7.0` (.NET7 SDK é requerido)