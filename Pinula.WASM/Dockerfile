FROM nginx:alpine

COPY ./publish/wwwroot /usr/share/nginx/html

RUN sed -i 's|try_files $uri $uri/ =404;|try_files $uri $uri/ /index.html;|g' /etc/nginx/conf.d/default.conf

EXPOSE 80