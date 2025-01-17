version: 1.0
builds:
  DedicatedGameServer: # replace with the name for your build
    executableName: server.x86_64 # the name of your build executable
    buildPath: builds/dedicated-server # the location of the build files
    excludePaths:
      - builds/dedicated-server/server_BackUpThisFolder_ButDontShipItWithYourGame/*.*
      - builds/dedicated-server/Competitive Action Multiplayer_BurstDebugInformation_DoNotShip/*.*
buildConfigurations:
  DedicatedGameServer: # replace with the name for your build configuration
    build: DedicatedGameServer # replace with the name for your build
    queryType: sqp # sqp or a2s, delete if you do not have logs to query
    binaryPath: server.x86_64 # the name of your build executable
    commandLine: -port $$port$$ -queryport $$query_port$$ -log $$log_dir$$/Engine.log # launch parameters for your server
    variables: {}
    readiness: true # server readiness feature flag
fleets:
  DedicatedGameServer: # replace with the name for your fleet
    buildConfigurations:
      - DedicatedGameServer # replace with the names of your build configuration
    regions:
      North America: # North America, Europe, Asia, South America, Australia
        minAvailable: 1 # minimum number of servers running in the region
        maxServers: 10 # maximum number of servers running in the region
