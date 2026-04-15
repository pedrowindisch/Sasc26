terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

# 1. Configure the AWS Provider (São Paulo region for lowest latency)
provider "aws" {
  region = "sa-east-1" 
}

# 2. Variables for your specific domain
variable "app_domain" {
  description = "The subdomain for the app and emails"
  type        = string
  default     = "sasc26.windisch.com.br" 
}

# 3. Set up a Security Group to allow web and SSH traffic
resource "aws_security_group" "web_sg" {
  name        = "sasc-checkin-sg"
  description = "Allow HTTP, HTTPS, and SSH traffic"

  ingress {
    description = "SSH from anywhere"
    from_port   = 22
    to_port     = 22
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    description = "HTTP from anywhere"
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    description = "HTTPS from anywhere"
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

# 4. Find the latest Ubuntu 24.04 LTS AMI (Free Tier Eligible)
data "aws_ami" "ubuntu" {
  most_recent = true
  owners      = ["099720109477"] # Canonical's official AWS account ID

  filter {
    name   = "name"
    values = ["ubuntu/images/hvm-ssd-gp3/ubuntu-noble-24.04-amd64-server-*"]
  }

  filter {
    name   = "virtualization-type"
    values = ["hvm"]
  }
}

# 5. Provision the EC2 Instance
resource "aws_instance" "app_server" {
  ami           = data.aws_ami.ubuntu.id
  instance_type = "t3.micro" 
  
  # IMPORTANT: Uncomment and add your AWS Key Pair name here to enable SSH access
  # key_name      = "your-aws-key-pair-name" 

  vpc_security_group_ids = [aws_security_group.web_sg.id]

  # User Data: The ultimate automation script runs once on server boot
  user_data = <<-EOF
              #!/bin/bash
              
              # A. Update and install core dependencies
              apt-get update -y
              apt-get install -y wget git apt-transport-https software-properties-common nginx certbot python3-certbot-nginx
              wget -q https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
              dpkg -i packages-microsoft-prod.deb
              rm packages-microsoft-prod.deb
              apt-get update -y
              apt-get install -y dotnet-sdk-8.0 aspnetcore-runtime-8.0

              # B. Clone your project from GitHub
              git clone https://github.com/pedrowindisch/Sasc26.git /var/www/sasc-app
              
              # C. Publish the app
              cd /var/www/sasc-app
              dotnet publish -c Release -o /var/www/sasc-published

              # D. Create the systemd service for the C# app
              cat << 'SERVICE' > /etc/systemd/system/sasc.service
              [Unit]
              Description=SASC 26 Check-In App

              [Service]
              WorkingDirectory=/var/www/sasc-published
              ExecStart=/usr/bin/dotnet /var/www/sasc-published/Sasc26.dll
              Restart=always
              RestartSec=10
              KillSignal=SIGINT
              SyslogIdentifier=sasc-app
              User=www-data
              Environment=ASPNETCORE_ENVIRONMENT=Production
              Environment=ASPNETCORE_URLS=http://localhost:5000

              [Install]
              WantedBy=multi-user.target
              SERVICE

              # E. Create the Nginx Reverse Proxy Configuration
              cat << 'NGINX_CONF' > /etc/nginx/sites-available/sasc26
              server {
                  listen 80;
                  server_name sasc26.windisch.com.br;

                  location / {
                      proxy_pass         http://localhost:5000;
                      proxy_http_version 1.1;
                      proxy_set_header   Upgrade \$http_upgrade;
                      proxy_set_header   Connection keep-alive;
                      proxy_set_header   Host \$host;
                      proxy_cache_bypass \$http_upgrade;
                      proxy_set_header   X-Forwarded-For \$proxy_add_x_forwarded_for;
                      proxy_set_header   X-Forwarded-Proto \$scheme;
                  }
              }
              NGINX_CONF

              # F. Enable Nginx site and remove default
              ln -s /etc/nginx/sites-available/sasc26 /etc/nginx/sites-enabled/
              rm -f /etc/nginx/sites-enabled/default
              
              # G. Start all services
              systemctl restart nginx
              systemctl enable sasc.service
              systemctl start sasc.service
              EOF

  tags = {
    Name = "SASC-CheckIn-Server"
  }
}

# 6. Attach an Elastic IP (Static IP)
resource "aws_eip" "app_ip" {
  instance = aws_instance.app_server.id
  domain   = "vpc"
  tags = { Name = "SASC-Static-IP" }
}

# 7. Configure SES for the Domain
resource "aws_ses_domain_identity" "sasc_domain" {
  domain = var.app_domain
}

# 8. Generate DKIM tokens for Email Deliverability
resource "aws_ses_domain_dkim" "sasc_dkim" {
  domain = aws_ses_domain_identity.sasc_domain.domain
}

# 9. Outputs for DNS Configuration
output "step_1_a_record" {
  description = "Point your subdomain to this IP address"
  value       = "Type: A | Name: sasc26 | Content: ${aws_eip.app_ip.public_ip}"
}

output "step_2_ses_verification_txt" {
  description = "TXT record to prove you own the domain to AWS"
  value       = "Type: TXT | Name: _amazonses.sasc26 | Content: ${aws_ses_domain_identity.sasc_domain.verification_token}"
}

output "step_3_dkim_cname_records" {
  description = "Create these 3 CNAME records to prevent emails going to spam"
  value = [
    for token in aws_ses_domain_dkim.sasc_dkim.dkim_tokens :
    "Type: CNAME | Name: ${token}._domainkey.sasc26 | Content: ${token}.dkim.amazonses.com"
  ]
}