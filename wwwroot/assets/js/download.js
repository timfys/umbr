angular.module('mekDownload', [])
    .controller('DownloadCtrl', ['$http', '$window', function ($http, $window) {
        var vm = this;

        vm.form = {};
        vm.form = {email: ''}; // Инициализация модели

// Функция для обрезки локальной части email
        vm.trimLocalPart = function () {
            if (!vm.form.email) return;

            const email = vm.form.email;
            const atIndex = email.indexOf('\u0040');

            if (atIndex === -1) {
                if (email.length > 64) {
                    vm.form.email = email.substring(0, 64);
                }
            } else {
                const localPart = email.substring(0, atIndex);
                if (localPart.length > 64) {
                    vm.form.email = localPart.substring(0, 64) + email.substring(atIndex);
                }
            }
        };
        vm.emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;
        vm.onlyNumbers = function ($event) {
            var keyCode = $event.which || $event.keyCode;
            // Разрешаем: backspace, delete, tab, escape, enter, стрелки
            if ([8, 9, 13, 27, 46, 37, 38, 39, 40].indexOf(keyCode) !== -1) {
                return;
            }
            // Разрешаем только цифры (0-9)
            if (keyCode < 48 || keyCode > 57) {
                $event.preventDefault();
            }
        };

        function getFileKey() {
            var path = $window.location.pathname; // например: /download/callcenterV7
            var parts = path.split('/').filter(function (p) {
                return p;
            });
            // parts: ["download", "callcenterV7"]

            var last = parts[parts.length - 1].toLowerCase();
            if (last === "download") {
                return "businessV7"; // дефолт
            }

            // берём всегда ПОСЛЕДНИЙ сегмент после /download/
            return parts[parts.length - 1];
        }

        vm.fileKey = getFileKey();

        vm.submit = function (form) {
            vm.submitted = true;

            if (!form.$valid) {
                return;
            }

            var payload = {
                name: vm.form.name,
                phone: vm.form.phone,
                email: vm.form.email,
                countryIso: 'RU',
                fileKey: getFileKey()
            };

            $http.post('/download/submit', payload)
                .then(function (response) {
                    var data = response.data;
                    if (data && data.success) {
                        window.location = data.url;
                    } else {
                        alert('Error: ' + (data && data.error ? data.error : 'Unknown error'));
                    }
                }, function (error) {
                    alert('Request error');
                });
        };
    }]);