terraform {
  required_version = ">= 1.5.0"

  required_providers {
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 3.0"
    }
  }
}

provider "kubernetes" {
  config_path    = pathexpand("~/.kube/config")
  config_context = "talentai"
}

resource "kubernetes_namespace_v1" "talentai" {
  metadata {
    name = "talentai"
    labels = {
      app        = "talentai"
      managed-by = "terraform"
    }
  }
}
