FROM ubuntu

RUN apt-get update
RUN apt-get install -y software-properties-common
RUN add-apt-repository -y ppa:webupd8team/java
RUN apt-get update

# Enable silent install
RUN echo "debconf shared/accepted-oracle-license-v1-1 select true" | debconf-set-selections
RUN echo "debconf shared/accepted-oracle-license-v1-1 seen true" | debconf-set-selections

RUN apt-get install -y oracle-java8-installer

# Not always necessary, but just in case...
RUN update-java-alternatives -s java-8-oracle

# Setting Java environment variables
RUN apt-get install oracle-java8-set-default


ENV SCALA_VERSION 2.12.1

RUN wget -q "http://downloads.lightbend.com/scala/$SCALA_VERSION/scala-$SCALA_VERSION.deb" && \
    ( dpkg -i "scala-$SCALA_VERSION.deb" || true ) && \
    apt-get update -y && apt-get install -y -f --no-install-recommends && \
    rm "scala-$SCALA_VERSION.deb" && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

ENV SBT_VERSION 0.13.13

RUN echo 'deb http://dl.bintray.com/sbt/debian /' > /etc/apt/sources.list.d/sbt.list &&\
    apt-get update && \
    apt-get install -y --force-yes sbt=0.13.13 && \
    apt-get clean autoremove -y && \
    rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/*

RUN sbt version

