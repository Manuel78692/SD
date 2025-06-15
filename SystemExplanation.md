# Explicação Detalhada do Sistema de Simulação WAVY

## Visão Geral do Sistema

Este projeto simula um sistema distribuído onde múltiplos dispositivos WAVY (simuladores de sensores) geram dados de diferentes tipos (temperatura, pH, humidade, GPS, giroscópio) e os enviam para um SERVIDOR central. Os dados não são enviados diretamente, mas sim através de um ou mais AGREGADORes, que atuam como intermediários. Os AGREGADORes recebem os dados dos WAVYs, solicitam o seu pré-processamento a um serviço RPC (PREPROCESSAMENTORPC) e, em seguida, reencaminham os dados pré-processados para o SERVIDOR. O SERVIDOR armazena os dados numa base de dados SQLite e em ficheiros CSV, e também expõe um serviço RPC para análise de dados. Uma interface de utilizador (MainUI) permite orquestrar o sistema, iniciar/parar os diferentes componentes e visualizar logs e dados.

## Fluxo de Dados

1.  **Geração de Dados (WAVY)**:
    *   Cada instância WAVY simula a recolha de dados de vários sensores (temperatura, pH, humidade, GPS, giroscópio).
    *   Os dados são gerados com base em localizações geográficas (cidades/regiões) e incluem um timestamp, tipo de sensor, unidade, valor e informação do WAVY (ID, localização, etc.).
    *   Os dados gerados são temporariamente armazenados num buffer local.
    *   Periodicamente, ou quando o buffer atinge um certo tamanho, os dados são enviados para uma exchange RabbitMQ específica do AGREGADOR. Cada WAVY é configurado para enviar dados para um AGREGADOR específico.

2.  **Receção e Pré-processamento (AGREGADOR)**:
    *   Cada AGREGADOR escuta uma fila RabbitMQ dedicada, para a qual os WAVYs publicam mensagens.
    *   Ao receber uma mensagem (um bloco de dados de um WAVY), o AGREGADOR extrai os dados.
    *   Para cada leitura de sensor individual, o AGREGADOR faz uma chamada RPC para o servidor PREPROCESSAMENTORPC.
    *   O servidor PREPROCESSAMENTORPC realiza o pré-processamento.
    *   Após receber a resposta do pré-processamento para todos os dados no bloco, o AGREGADOR envia o bloco de dados pré-processados para o SERVIDOR via uma ligação TCP.

3.  **Armazenamento e Análise (SERVIDOR)**:
    *   O SERVIDOR escuta ligações TCP de AGREGADORes numa porta configurada.
    *   Ao receber um bloco de dados pré-processados de um AGREGADOR, o SERVIDOR:
        *   Armazena cada leitura de sensor na base de dados SQLite. É utilizada uma estratégia de Tabela Por Tipo (TPT), onde existe uma tabela base `SensorReadings` e tabelas separadas para cada tipo de sensor (`TemperatureReadings`, `PhReadings`, etc.).
        *   Adicionalmente, os dados de cada tipo de sensor são também guardados em ficheiros CSV separados (e.g., `temperatura.csv`, `gps.csv`).
    *   O SERVIDOR também expõe um serviço RPC (`AnaliseRPCServer`) que pode ser utilizado para realizar análises sobre os dados armazenados (por exemplo, calcular médias, encontrar leituras anómalas, etc.). A `MainUI` pode interagir com este serviço.

4.  **Orquestração e Visualização (MainUI)**:
    *   A `MainUI` permite ao utilizador iniciar e parar as várias componentes do sistema (WAVYs, AGREGADORes, SERVIDOR, PREPROCESSAMENTORPC).
    *   Fornece feedback visual sobre o estado do sistema, logs de operações e pode ser usada para consultar dados ou resultados de análises (interagindo com o `AnaliseRPCServer` do SERVIDOR).

## Componentes Detalhados

### WAVY (`WAVY/`)

*   **`WavyMain.cs`**: Ponto de entrada, configura e inicia as instâncias de WAVYs.
*   **`Wavy.cs`**: Lógica principal do WAVY.
    *   Gera dados de sensores através de `SimulatorFactory` e simuladores específicos (`SimuladorGPS`, `SimuladorTemperatura`, etc.).
    *   Os simuladores (`geradores/`) criam dados realistas ou pseudo-aleatórios para cada tipo de sensor. Por exemplo, `SimuladorGPS` usa `RandomCityRegion` para gerar coordenadas e nomes de locais.
    *   Utiliza RabbitMQ para enviar dados para um AGREGADOR específico. A configuração do RabbitMQ (host, exchange) é definida em `RabbitMqConstants.cs`.
    *   Implementa um buffer para agrupar leituras de sensores antes de as enviar.
*   **`SimulatorFactory.cs`**: Cria instâncias dos diferentes simuladores de sensores.
*   **`RabbitMqConstants.cs`**: Constantes para a ligação RabbitMQ.

### AGREGADOR (`AGREGADOR/`)

*   **`AgregadorMain.cs`**: Ponto de entrada, configura e inicia as instâncias de AGREGADORes.
*   **`Agregador.cs`**: Lógica principal do AGREGADOR.
    *   Conecta-se ao RabbitMQ para receber dados dos WAVYs.
    *   Para cada bloco de dados recebido:
        *   Deserializa as mensagens.
        *   Itera sobre as leituras de sensor e, para cada uma, faz uma chamada RPC para o `PREPROCESSAMENTORPC` para pré-processamento.
        *   Após o pré-processamento de todas as leituras no bloco, envia o bloco de dados para o SERVIDOR via TCP.
*   **`PreProcessamentoModels.cs`**: Modelos de dados usados na comunicação RPC com o `PREPROCESSAMENTORPC`.
*   **`RabbitMqConstants.cs`**: Constantes para a ligação RabbitMQ.

### PREPROCESSAMENTORPC (`PREPROCESSAMENTORPC/`)

*   **`PreProcessamentoRPCServerMain.cs`**: Ponto de entrada, inicia o servidor RPC de pré-processamento.
*   **`PreProcessamentoRPCServer.cs`**: Implementação do servidor RPC.
    *   Recebe pedidos de pré-processamento dos AGREGADORes.
    *   A lógica de pré-processamento atual consiste em separar o bloco cru de dados enviado pelos WAVYs em blocos de tipos de dados (gps, temperatura, etc.).
    *   Retorna os dados pré-processados (ou um estado de sucesso/falha) ao AGREGADOR.

### SERVIDOR (`SERVIDOR/`)

*   **`SevidorMain.cs`**: Ponto de entrada, inicia o SERVIDOR e o seu servidor RPC de análise.
*   **`Servidor.cs`**: Lógica principal do SERVIDOR.
    *   Escuta ligações TCP de AGREGADORes.
    *   Ao receber dados:
        *   Utiliza `SensorDataService` para guardar os dados.
*   **`Services/SensorDataService.cs`**:
    *   Responsável por interagir com a base de dados (`SensorDataContext`) e escrever nos ficheiros CSV.
    *   Guarda os dados na base de dados SQLite usando Entity Framework Core.
    *   Escreve os dados em ficheiros CSV específicos para cada tipo de sensor.
*   **`Data/SensorDataContext.cs`**: Contexto do Entity Framework Core para a base de dados. Define as tabelas e relações.
    *   Usa uma estratégia TPT (Table-Per-Type) com `SensorReading` como base e tabelas especializadas como `TemperatureReading`, `GpsReading`, etc.
*   **`Data/DatabaseConfig.cs`**: Configurações da base de dados, como o nome do ficheiro SQLite.
*   **`Models/`**: Contém as classes que representam os diferentes tipos de leituras de sensores (`SensorReading.cs`, `TemperatureReading.cs`, etc.).
*   **`AnaliseRPCServer.cs`**: Implementação do servidor RPC para análise de dados. Permite que clientes (como a `MainUI`) solicitem análises sobre os dados armazenados.
*   **`AnaliseRPCClient.cs`**: Um cliente para o `AnaliseRPCServer`, possivelmente usado pela `MainUI` ou para testes.
*   **`DatabaseVerifier.cs`**: Utilitário para verificar o estado da base de dados.

### MainUI (`MainUI/`)

*   **`MainUI.cs`**: Interface de utilizador baseada em consola.
    *   Permite ao utilizador iniciar/parar os WAVYs e/ou AGREGADORes.
    *   Mostra logs e informações de estado.
    *   Pode interagir com o `AnaliseRPCServer` para pedir e mostrar resultados de análises.

## Geração de Dados dos Sensores (Breve Descrição)

O sistema simula diversos tipos de sensores:

*   **Temperatura (`SimuladorTemperatura.cs`)**: Gera valores de temperatura (e.g., em Celsius) dentro de um intervalo razoável, possivelmente com alguma variação aleatória.
*   **pH (`SimuladorPH.cs`)**: Gera valores de pH (e.g., entre 0 e 14) com variação.
*   **Humidade (`SimuladorHumidade.cs`)**: Gera valores percentuais de humidade.
*   **GPS (`SimuladorGPS.cs`)**:
    *   Utiliza `RandomCityRegion.cs` para selecionar aleatoriamente uma cidade e região de uma lista predefinida.
    *   Gera coordenadas (latitude, longitude) associadas a essa localização.
*   **Giroscópio (`SimuladorGyro.cs`)**: Gera valores para os eixos X, Y, Z, simulando dados de movimento ou orientação.

Cada simulador é responsável por criar instâncias dos respetivos modelos de dados (e.g., `TemperatureReading`, `GpsReading`) preenchidos com os valores simulados, um timestamp, e identificadores do WAVY.

## Possíveis Melhorias e Problemas

### Melhorias Potenciais

1.  **Segurança**:
    *   **Comunicação**: Implementar TLS/SSL para as ligações TCP entre AGREGADOR e SERVIDOR, e para as chamadas RPC.
    *   **RabbitMQ**: Usar autenticação e autorização no RabbitMQ.
    *   **Autenticação de Componentes**: Garantir que apenas componentes autorizados podem comunicar entre si.

2.  **Escalabilidade e Robustez**:
    *   **Balanceamento de Carga para AGREGADORes**: Atualmente, um WAVY envia para um AGREGADOR específico. Poderia ser implementado um mecanismo para distribuir WAVYs por AGREGADORes dinamicamente ou usar um balanceador de carga na frente dos AGREGADORes.
    *   **Filas de Contingência (Dead Letter Queues)**: No RabbitMQ, configurar DLQs para mensagens que não podem ser processadas pelos AGREGADORes, permitindo análise e reprocessamento.
    *   **Tolerância a Falhas no SERVIDOR**: Se o SERVIDOR estiver offline, os AGREGADORes podem perder dados. Implementar um mecanismo de retentativa com backoff exponencial nos AGREGADORes ou uma fila persistente no lado do AGREGADOR.
    *   **Replicação da Base de Dados**: Para maior disponibilidade e robustez dos dados no SERVIDOR.
    *   **Escalabilidade do PREPROCESSAMENTORPC**: Se o pré-processamento for complexo, este serviço pode tornar-se um bottleneck. Considerar múltiplas instâncias e balanceamento de carga.

3.  **Pré-processamento Avançado**:
    *   Expandir as capacidades do `PREPROCESSAMENTORPC` para incluir validação mais sofisticada, limpeza de dados, transformação, enriquecimento (e.g., adicionar informações de geolocalização reversa a partir de coordenadas GPS).

4.  **Monitorização e Logging**:
    *   Implementar um sistema de logging centralizado (e.g., ELK stack, Seq).
    *   Adicionar métricas de desempenho para cada componente (e.g., taxa de processamento de mensagens, latência RPC, uso de CPU/memória).

5.  **Configuração**:
    *   Externalizar mais configurações (portas, hosts, nomes de filas/exchanges, connection strings) para ficheiros de configuração (e.g., `appsettings.json`) em vez de constantes no código.

6.  **Interface de Utilizador**:
    *   Desenvolver uma `MainUI` mais rica, talvez baseada na web ou desktop, para melhor visualização de dados, gestão do sistema e alertas.

7.  **Análise de Dados**:
    *   Expandir as funcionalidades do `AnaliseRPCServer` com mais tipos de análises (e.g., deteção de anomalias em tempo real, tendências, agregações complexas).

8.  **Formato de Dados**:
    *   Considerar formatos de serialização mais eficientes e padronizados como Protocol Buffers ou Avro em vez de JSON, especialmente para grandes volumes de dados.

### Problemas Potenciais (e Considerações)

1.  **Perda de Dados**:
    *   **WAVY para AGREGADOR**: Se o RabbitMQ estiver indisponível quando um WAVY tenta enviar, ou se a mensagem não for entregue/confirmada, os dados podem ser perdidos se não houver lógica de retentativa robusta no WAVY.
    *   **AGREGADOR para SERVIDOR**: Se o SERVIDOR estiver indisponível quando um AGREGADOR tenta enviar via TCP, os dados podem ser perdidos se o AGREGADOR não tiver um buffer persistente ou mecanismo de retentativa.
    *   **Falha no AGREGADOR**: Se um AGREGADOR falhar após receber uma mensagem do RabbitMQ mas antes de a enviar com sucesso para o SERVIDOR (e antes de confirmar a mensagem ao RabbitMQ, dependendo do modo de acknowledgement), a mensagem pode ser perdida ou reprocessada (causando duplicados se não for idempotente).

2.  **Processamento de Mensagens Duplicadas**:
    *   Se o acknowledgement do RabbitMQ falhar após o processamento pelo AGREGADOR, a mensagem pode ser reentregue, levando a dados duplicados no SERVIDOR se o processamento não for idempotente. O SERVIDOR precisaria de uma forma de detetar e lidar com duplicados.

3.  **Bottlenecks**:
    *   **PREPROCESSAMENTORPC**: Se o pré-processamento for síncrono e demorado, pode atrasar os AGREGADORes.
    *   **SERVIDOR**: A escrita na base de dados e nos ficheiros CSV pode tornar-se um bottleneck com muitos AGREGADORes a enviar dados simultaneamente. O uso de `async/await` e batching pode ajudar.
    *   **RabbitMQ**: A performance do broker RabbitMQ pode ser um fator.

4.  **Consistência de Dados**:
    *   A escrita em CSV e na base de dados pelo SERVIDOR são duas operações separadas. Se uma falhar e a outra não, pode haver inconsistência. Idealmente, estas operações deveriam ser atómicas ou ter um mecanismo de compensação.

5.  **Gestão de Estado e Configuração**:
    *   A `MainUI` parece ser responsável por iniciar os componentes. Num sistema de produção, seria necessário um orquestrador mais robusto (e.g., Kubernetes, systemd services).
    *   A descoberta de serviços (e.g., onde está o PREPROCESSAMENTORPC, onde está o SERVIDOR) parece ser baseada em configurações fixas. Num ambiente dinâmico, um registo/descoberta de serviços (e.g., Consul, etcd) seria benéfico.

6.  **Sincronização de Tempo**:
    *   Os timestamps são gerados pelos WAVYs. É crucial que os relógios dos WAVYs estejam sincronizados (e.g., via NTP) para que a ordem e a correlação temporal dos eventos sejam precisas.

7.  **Complexidade da Lógica de Negócio nos AGREGADORes**:
    *   Os AGREGADORes fazem chamadas RPC síncronas para cada leitura. Para um bloco grande de dados, isto pode ser ineficiente. Considerar chamadas RPC em batch ou processamento assíncrono dentro do AGREGADOR.

Este documento fornece uma base para entender o sistema.
