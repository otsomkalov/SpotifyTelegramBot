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
  default = "<Spotify login callback url>"
}

variable "database-connection-string" {
  type = string
}

variable "env" {
  type    = string
  default = "prd"
}