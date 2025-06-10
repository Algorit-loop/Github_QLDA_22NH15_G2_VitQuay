/**
 * Script để sửa vấn đề z-index giữa các phần tử trên trang
 * Bao gồm footer, form đăng nhập/đăng ký, và chatbot
 */
document.addEventListener('DOMContentLoaded', function() {
    // Khởi tạo fix cho chatbot
    initChatbotFix();
    
    // Điều chỉnh vị trí của footer và form khi trang đăng nhập/đăng ký đang hoạt động
    const currentPath = window.location.pathname;
    
    // Kiểm tra xem có đang ở trang đăng nhập hay đăng ký không
    if (currentPath.includes('/Account/Login') || currentPath.includes('/Account/Register')) {
        // Ẩn tạm footer khi ở trang đăng nhập/đăng ký
        const footer = document.querySelector('.footer');
        if (footer) {
            footer.style.position = 'relative';
            footer.style.zIndex = '10';
        }
        
        // Đảm bảo form hiển thị đúng
        const authPage = document.querySelector('.auth-page');
        if (authPage) {
            authPage.style.position = 'relative';
            authPage.style.zIndex = '2000';
            authPage.style.backgroundColor = '#f8f9fa';
        }
        
        // Đảm bảo wrapper hiển thị đúng
        const authWrapper = document.querySelector('.auth-wrapper');
        if (authWrapper) {
            authWrapper.style.position = 'relative';
            authWrapper.style.zIndex = '2000';
            authWrapper.style.backgroundColor = 'white';
        }
    }

    // Sửa vấn đề với modal z-index
    const modalButtons = document.querySelectorAll('[data-bs-toggle="modal"]');
    modalButtons.forEach(function(button) {
        button.addEventListener('click', function() {
            setTimeout(function() {
                const modalBackdrops = document.querySelectorAll('.modal-backdrop');
                modalBackdrops.forEach(function(backdrop) {
                    backdrop.style.zIndex = '1050';
                });
                
                const modals = document.querySelectorAll('.modal.show');
                modals.forEach(function(modal) {
                    modal.style.zIndex = '2000';
                });                // Ensure chatbot is below modals when modal is open
                const chatbotContainer = document.querySelector('.chatbot-container');
                if (chatbotContainer && modals.length > 0) {
                    chatbotContainer.style.zIndex = '1000'; // Lower z-index when modal is open
                    chatbotContainer.classList.add('pointer-events-none'); // Vô hiệu hóa event khi có modal
                    console.log('[Fix-Z-Index] Modal opened - disabling chatbot pointer events');
                }
                
                // Fix vấn đề tương tác trong modal
                document.querySelectorAll('.modal.show, .modal.show .modal-dialog, .modal.show .modal-content').forEach(function(element) {
                    element.style.zIndex = '2050';
                });
                
                // Đảm bảo tất cả các phần tử trong modal có thể nhấn được
                document.querySelectorAll('.modal.show .btn, .modal.show button, .modal.show input, .modal.show a, .modal.show select, .modal.show .form-control').forEach(function(element) {
                    element.style.position = 'relative';
                    element.style.zIndex = '2100';
                });
            }, 10);
        });
    });      // Restore chatbot z-index when modal is closed
    document.addEventListener('hidden.bs.modal', function() {
        setTimeout(function() {
            const modals = document.querySelectorAll('.modal.show');
            if (modals.length === 0) {
                // No modals visible, restore chatbot z-index
                const chatbotContainer = document.querySelector('.chatbot-container');
                if (chatbotContainer) {
                    chatbotContainer.style.zIndex = '1500';
                    // Kiểm tra trạng thái chatbot trước khi bật lại sự kiện
                    const chatbotPopup = document.querySelector('.chatbot-popup');
                    if (!chatbotPopup || !chatbotPopup.classList.contains('active')) {
                        chatbotContainer.classList.add('pointer-events-none');
                    } else {
                        chatbotContainer.classList.remove('pointer-events-none');
                    }
                    console.log('[Fix-Z-Index] Modal closed - restoring chatbot state');
                }
            }
        }, 100);
    });
});

/**
 * Hàm khởi tạo fix cho chatbot
 * Giải quyết vấn đề không nhấp được vào nút khi gần chatbot
 */
function initChatbotFix() {
    // Thêm class 'pointer-events-fix' vào thẻ chatbot-container khi không tương tác
    const chatbotContainer = document.querySelector('.chatbot-container');
    const chatbotLauncher = document.getElementById('chatbot-launcher');
    const chatbotPopup = document.querySelector('.chatbot-popup');

    if (chatbotContainer && chatbotLauncher && chatbotPopup) {
        console.log('[Fix-Z-Index] Initializing chatbot pointer-events fix');
        
        // Mặc định, cho phép click xuyên qua khi chatbot đóng
        chatbotContainer.classList.add('pointer-events-fix');

        // Theo dõi trạng thái mở/đóng của chatbot để điều chỉnh hiệu ứng pointer-events
        const toggleChatbotObserver = new MutationObserver(function(mutations) {
            mutations.forEach(function(mutation) {
                if (mutation.type === 'attributes' && mutation.attributeName === 'class') {
                    if (chatbotPopup.classList.contains('active')) {
                        // Nếu chatbot đang mở, bỏ class pointer-events-fix
                        chatbotContainer.classList.remove('pointer-events-fix');
                        console.log('[Fix-Z-Index] Chatbot active - enabling pointer events');
                    } else {
                        // Nếu chatbot đang đóng, thêm class pointer-events-fix
                        chatbotContainer.classList.add('pointer-events-fix');
                        console.log('[Fix-Z-Index] Chatbot inactive - disabling pointer events');
                    }
                }
            });
        });

        // Chỉ theo dõi thay đổi class của popup
        toggleChatbotObserver.observe(chatbotPopup, { attributes: true });

        // Đảm bảo nút launcher luôn có thể nhấp vào
        chatbotLauncher.addEventListener('mouseenter', function() {
            chatbotContainer.classList.remove('pointer-events-fix');
        });
            
        chatbotLauncher.addEventListener('mouseleave', function() {
            if (!chatbotPopup.classList.contains('active')) {
                chatbotContainer.classList.add('pointer-events-fix');
            }
        });
        
        console.log('[Fix-Z-Index] Chatbot z-index fix initialized');
    } else {
        console.warn('[Fix-Z-Index] Could not find chatbot elements');
    }
}
