
var wenewsApp = angular.module('wenewsApp', ['ngRoute', 'ngResource', 'ui.bootstrap']);
var urlapi="http://localhost:8080/grid/";
wenewsApp.config(function($routeProvider) {
  $routeProvider .when('/home', {
                controller: 'homeController',
                templateUrl: 'pages/home.html',
                controllerAs: 'vm'
            })
			 .when('/', {
                controller: 'homeController',
                 templateUrl: 'pages/home.html',
                controllerAs: 'vm'
            })
           
			
});
wenewsApp.service('searchService', function() {
  this.searchText = '';

});

wenewsApp.controller('homeController', ['$scope','$http','$sce','$interval', function($scope, $http,$sce,$interval) {
  
	$scope.trustSrc = function(src) {
		return $sce.trustAsResourceUrl(src);
	};
	$scope.latestPost = [];
	$scope.videopost = [];
	
	$scope.filteredlatestPost = [];
	$scope.filteredvideopost = [];
	
	$scope.numPerPage = 4;
	$scope.currentPage = 1;
	$scope.maxSize = 5; 
	
	$scope.vnumPerPage = 2;
	$scope.vcurrentPage = 1;	


	$scope.changePage = function()
	{
		
	};
	$scope.changePageVideo = function()
	{
	
		
	};
	toastr.options = {
		  "closeButton": false,
		  "debug": false,
		  "newestOnTop": true,
		  "progressBar": false,
		  "positionClass": "toast-top-right",
		  "preventDuplicates": false,
		  "onclick": null,
		  "showDuration": "300",
		  "hideDuration": "1000",
		  "timeOut": "5000",
		  "extendedTimeOut": "1000",
		  "showEasing": "swing",
		  "hideEasing": "linear",
		  "showMethod": "fadeIn",
		  "hideMethod": "fadeOut"
		};
  
	$scope.reload = function () {
		 $http.get('data.json?t='+new Date().getTime()).success(function(data) {
			//console.log(data);
			$scope.ms = data;
			$scope.count();
        });		
	  };
	$scope.count = function () {				
		if(typeof($scope.ms) == "undefined" || $scope.ms == null || $scope.ms.forEach ==null)
			return;
		//console.log($scope.ms);
		var d = new Date().getTime();		
		$scope.ms.forEach(function(m)
		{			
			if(m==null || m.time ==0)
					return;
			var distance = d - m.time;			
			var hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
			var minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
			var seconds = Math.floor((distance % (1000 * 60)) / 1000);
			
			hours=('0' + hours).slice(-2);
			minutes=('0' + minutes).slice(-2);
			seconds=('0' + seconds).slice(-2);			
			m.timer = hours+":"+ minutes+":"+seconds;			
			m.state = m.status == 1 ?"Connected":"Not Connect";
		});		
	  };
	  $scope.reset = function (deviceId) {
		  
		   $http.get('reset?id='+deviceId).success(function(data) {
			   if(data.success)
			   {
				   toastr['success']("reset time: "+data.time+"ms",data.message);
					$scope.reload();
			   }
			   else
			   {
				   toastr["error"](data.message);
			   }				
        });		
	  };
	 $scope.stop = function (deviceId) {
		  
		   $http.get('stop?id='+deviceId).success(function(data) {
			   if(data.success)
			   {
				 toastr['success'](data.message);
				 $scope.reload();
			   }
			   else
			   {
				   toastr["error"](data.message);
			   }
				
        });		
	  };
	 $scope.copyAllProxy = function () {				
		if(typeof($scope.ms) == "undefined" || $scope.ms == null || $scope.ms.forEach ==null)
			return;		
		var textAllProxy="";
		$scope.ms.forEach(function(m)
		{			
			if(m==null || m.time ==0)
					return;		
				textAllProxy+=m.proxy_address+"\r\n";
		});
		$scope.copyToClipboard(textAllProxy);
	  };
	$scope.copyToClipboard = function (strText) {	
			const el = document.createElement('textarea');
			el.value = strText;
			document.body.appendChild(el);
			el.select();
			document.execCommand('copy');
			document.body.removeChild(el);
	}

	$interval($scope.count, 1100);
	$interval($scope.reload, 20000);
    $scope.reload();
	
		
		
		
	$scope.$watch('currentPage + numPerPage', function() {
		$scope.changePage();	  
	});
	$scope.$watch('vcurrentPage + vnumPerPage', function() {
		$scope.changePageVideo();	  
	});
    /* $scope.$watch('curPage + numPerPage', function() {
    var begin = (($scope.curPage - 1) * $scope.itemsPerPage),
    end = begin + $scope.itemsPerPage;
    
    $scope.filteredItems = $scope.latestPost.slice(begin, end);
  });*/
		
	/*
	$scope.searchText = searchService.searchText;  
	$scope.$watch('searchText', function() {
		searchService.searchText = $scope.searchText;
		//console.log(searchService.searchText);
  });*/
  
}]);
