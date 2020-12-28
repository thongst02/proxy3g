# proxy3g
Sử dụng dcom làm proxy 3g. Dùng những con android box cài Armbian (tùy chip của box mà dùng bản nào cho phù hợp, các bạn tự search về Armbian nhé.)
Cài 3proxy (https://3proxy.ru/).
Cài wvdial.
Quan trọng nhất vẫn là dotnet runtime 3.0.0.

Sau khi cài hết những thứ bên trên,
-Set cứng ip wifi.
Mở port firewall
<pre>
<code>
 sudo ufw status
 sudo ufw allow http
 sudo ufw allow 22/tcp
 </code></pre>
 cài thành công hết thì chỉ cần cd đến thư mục chứa dll và chạy lệnh:
 <pre>
 <code>
 ./ShellHelper
  </code></pre>
 Để chạy được tự động thì bạn có thể set nó thành service hoặc là set vào startup (phần này các bạn google run on start up ubuntu - armbian) để hiểu rõ hơn.
