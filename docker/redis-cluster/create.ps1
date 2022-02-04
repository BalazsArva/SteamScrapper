param(
    [Int32]$Masters = 3,
    [Int32]$ReplicationFactor = 3,
    [Int32]$PortOffset = 37000,
    [Int32]$UIPort = 8001)

$ComposeFileName = "docker-compose-$Masters-$ReplicationFactor.yml"
if (Test-Path -Path ".\$ComposeFileName") {
    Write-Host "Exists"
    del ".\$ComposeFileName"
}


Write "version: '3.8'" >> $ComposeFileName
Write "" >> $ComposeFileName
Write "services:" >> $ComposeFileName

Write "  redis-gateway:" >> $ComposeFileName
Write "    container_name: redis-gateway" >> $ComposeFileName
Write "    image: alpine:latest" >> $ComposeFileName
Write "    restart: unless-stopped" >> $ComposeFileName
Write "    ports:" >> $ComposeFileName
Write "      - ""${UIPort}:8001""" >> $ComposeFileName

for ($i = 1; $i -le ($Masters * $ReplicationFactor); ++$i) {
    $p = $i + $PortOffset;
    Write "      - ""${p}:${p}""" >> $ComposeFileName
}

Write "    entrypoint: [ ""sleep"", ""infinity"" ]" >> $ComposeFileName
Write "" >> $ComposeFileName

for ($i = 1; $i -le ($Masters * $ReplicationFactor); ++$i) {
    $p = $i + $PortOffset;
    Write "  redis-cluster-${i}:" >> $ComposeFileName

    Write "    depends_on:" >> $ComposeFileName
    Write "      - redis-gateway" >> $ComposeFileName
    Write "    container_name: redis-cluster-${i}" >> $ComposeFileName
    Write "    image: redis:6" >> $ComposeFileName
    Write "    restart: unless-stopped" >> $ComposeFileName
    Write "    network_mode: 'service:redis-gateway'" >> $ComposeFileName
    Write "    entrypoint: [ ""redis-server"", ""--port"", ""${p}"", ""--cluster-enabled"", ""yes"", ""--cluster-config-file"", ""nodes.conf"", ""--cluster-node-timeout"", ""10000"" ]" >> $ComposeFileName
    Write "" >> $ComposeFileName   
}

Write "  redis-cluster-ui:" >> $ComposeFileName
Write "    depends_on:" >> $ComposeFileName
Write "      - redis-gateway" >> $ComposeFileName
Write "    container_name: redis-cluster-ui" >> $ComposeFileName
Write "    restart: unless-stopped" >> $ComposeFileName
Write "    image: redislabs/redisinsight" >> $ComposeFileName
Write "    network_mode: 'service:redis-gateway'" >> $ComposeFileName








Invoke-Expression -Command "docker-compose -f $ComposeFileName up -d"
$InitialPort = $($PortOffset + 1)

$ClusterSetupCommand = "docker exec -it redis-cluster-1 redis-cli -h 127.0.0.1 -p ${InitialPort} --cluster create "

for ($i = 1; $i -le ($Masters * $ReplicationFactor); ++$i) {
    $p = $($PortOffset + $i)
    $ClusterSetupCommand += "127.0.0.1:${p} "
}
$NumberOfReplicas = $($ReplicationFactor - 1)
$ClusterSetupCommand += "--cluster-replicas ${NumberOfReplicas}"


Invoke-Expression -Command $ClusterSetupCommand
