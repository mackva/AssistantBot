FROM mcr.microsoft.com/dotnet/aspnet:6.0
EXPOSE 80

ENV PORT 80

RUN mkdir /app
WORKDIR /app
COPY . .
ENTRYPOINT ["dotnet", "Telegram.Bot.Examples.WebHook.dll"]