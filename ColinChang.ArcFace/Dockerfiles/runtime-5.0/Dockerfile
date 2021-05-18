FROM mcr.microsoft.com/dotnet/runtime:5.0-focal

# .net 全球化
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

RUN apt-get -yqq update && apt-get -yqq upgrade
RUN apt-get install gnupg ca-certificates -y
RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
RUN echo "deb https://download.mono-project.com/repo/ubuntu stable-focal main" | tee /etc/apt/sources.list.d/mono-official-stable.list
RUN apt-get update -y && apt-get install mono-devel -y && apt-get install libgdiplus/focal -y
RUN apt-get install vim -y

COPY ../../Sdks/Linuxx64/*.so /lib/