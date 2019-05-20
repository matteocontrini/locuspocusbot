workflow "Build and push" {
  on = "push"
  resolves = ["Docker Push"]
}

action "Docker Build" {
  uses = "actions/docker/cli@8cdf801b322af5f369e00d85e9cf3a7122f49108"
  args = "build -t bot ."
}

action "Docker Tag" {
  uses = "actions/docker/tag@8cdf801b322af5f369e00d85e9cf3a7122f49108"
  needs = ["Docker Build"]
  args = "bot matteocontrini/locuspocusbot"
}

action "Docker Login" {
  uses = "actions/docker/login@8cdf801b322af5f369e00d85e9cf3a7122f49108"
  needs = ["Docker Tag"]
  secrets = [
    "DOCKER_USERNAME",
    "DOCKER_PASSWORD",
  ]
}

action "Docker Push" {
  uses = "actions/docker/cli@8cdf801b322af5f369e00d85e9cf3a7122f49108"
  needs = ["Docker Login"]
  args = "push matteocontrini/locuspocusbot"
}
