FROM mcr.microsoft.com/dotnet/runtime:6.0
LABEL version="runtime-6.0" maintainer="colinchang<zhangcheng5468@gmail.com>"

COPY ./ColinChang.ArcFace/Sdks/Linuxx64/*.so /lib/

RUN apt update \
&& apt upgrade -y \
&& apt install -y apt-transport-https dirmngr gnupg ca-certificates \
&& apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF \
&& echo "deb https://download.mono-project.com/repo/debian stable-buster main" | tee /etc/apt/sources.list.d/mono-official-stable.list \
&& apt update \
&& apt install -y mono-devel libgdiplus/stable-buster \
&& apt clean

# .net 全球化
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false