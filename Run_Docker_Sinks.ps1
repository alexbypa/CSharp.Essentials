# Avvio di Seq
Write-Host "Avvio del container Seq..." -ForegroundColor Cyan
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -e SEQ_FIRSTRUN_ADMINPASSWORD="SuperSecretPassword123!" -p 5341:80 datalust/seq:latest

# Avvio di Elasticsearch
Write-Host "Avvio del container Elasticsearch..." -ForegroundColor Cyan
docker run -d `
  --name elasticsearch `
  -p 9200:9200 `
  -p 9300:9300 `
  -e "discovery.type=single-node" `
  -e "xpack.security.enabled=false" `
  -e "ES_JAVA_OPTS=-Xms1g -Xmx1g" `
  docker.elastic.co/elasticsearch/elasticsearch:8.14.3

# Breve pausa per permettere il bootstrap di Elasticsearch
Write-Host "Attendo 15 secondi per l'avvio del motore di Elasticsearch..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

# Avvio di Kibana
Write-Host "Avvio del container Kibana..." -ForegroundColor Cyan
docker run -d `
  --name kibana `
  -p 5601:5601 `
  -e "ELASTICSEARCH_HOSTS=http://host.docker.internal:9200" `
  docker.elastic.co/kibana/kibana:8.14.3

Write-Host "Completato! Controlla lo stato dei container con 'docker ps'." -ForegroundColor Green