FROM mcr.microsoft.com/dotnet/runtime:3.1-focal
LABEL version="runtime-5.0" maintainer="colinchang<zhangcheng5468@gmail.com>"

COPY ./ColinChang.ArcFace/Sdks/Linuxx64/*.so /lib/

RUN apt update \
&& apt upgrade -y \
&& apt install gnupg ca-certificates -y \
&& apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF \
&& echo "deb https://download.mono-project.com/repo/ubuntu stable-focal main" | tee /etc/apt/sources.list.d/mono-official-stable.list \
&& apt update \
&& apt install mono-devel -y \
&& apt install libgdiplus/focal -y \
&& apt clean

# .net 全球化
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false