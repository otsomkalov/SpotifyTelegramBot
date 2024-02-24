variable "telegram-token" {
  type = string
}

variable "telegram-bot-url" {
  type = string
}

variable "spotify-client-id" {
  type = string
}

variable "spotify-client-secret" {
  type = string
}

variable "spotify-callback-url" {
  type = string
}

variable "database-connection-string" {
  type = string
}

variable "database-name" {
  type = string
}

variable "env" {
  type    = string
  default = "prd"
}