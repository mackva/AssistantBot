FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-alpine-amd64

EXPOSE 8443

RUN mkdir /app
WORKDIR /app
COPY ./linux64_musl/. ./

RUN chmod +x ./Telegram.Bot.Examples.WebHook.dll
CMD ["./Telegram.Bot.Examples.WebHook.dll", "--urls", "http://0.0.0.0:8443"]