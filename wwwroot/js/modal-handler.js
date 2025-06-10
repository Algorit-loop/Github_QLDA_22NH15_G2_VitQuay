/**
 * Modal Interaction Handler
 * Xử lý các vấn đề tương tác giữa modal và chatbot
 */

document.addEventListener('DOMContentLoaded', function() {
    // Theo dõi sự kiện mở modal
    document.querySelectorAll('[data-bs-toggle="modal"]').forEach(button => {
        button.addEventListener('click', function() {
            console.log('[Modal-Handler] Modal button clicked');
            setTimeout(handleModalOpen, 50);
        });
    });

    // Khi có modal mở, theo dõi sự kiện
    document.addEventListener('shown.bs.modal', function(event) {
        console.log('[Modal-Handler] Modal shown event detected');
        handleModalOpen();
    });

    // Khi modal đóng
    document.addEventListener('hidden.bs.modal', function() {
        console.log('[Modal-Handler] Modal hidden event detected');
        setTimeout(handleModalClose, 50);
    });

    // Xử lý khi modal mở
    function handleModalOpen() {
        console.log('[Modal-Handler] Processing modal open');
        const modalElements = document.querySelectorAll('.modal.show');

        if (modalElements.length > 0) {
            // Thêm class cho body
            document.body.classList.add('modal-open');

            // Làm việc với từng modal
            modalElements.forEach(modal => {
                // Đảm bảo modal có z-index cao
                modal.style.zIndex = '2050';
                
                // Tăng z-index cho các phần tử con
                const modalDialog = modal.querySelector('.modal-dialog');
                if (modalDialog) modalDialog.style.zIndex = '2051';
                
                const modalContent = modal.querySelector('.modal-content');
                if (modalContent) modalContent.style.zIndex = '2052';
                
                // Tăng z-index cho các phần tử tương tác
                modal.querySelectorAll('button, .btn, input, select, a, .form-control').forEach(elem => {
                    elem.style.zIndex = '2060';
                    elem.style.position = 'relative';
                    // Đảm bảo rằng pointer-events là auto
                    elem.style.pointerEvents = 'auto';
                });

                // Đảm bảo dropdown hoạt động đúng
                modal.querySelectorAll('.dropdown-menu').forEach(menu => {
                    menu.style.zIndex = '2070';
                });
                
                // Vô hiệu hóa chatbot tạm thời
                const chatbot = document.querySelector('.chatbot-container');
                if (chatbot) {
                    chatbot.style.zIndex = '1000';
                    chatbot.classList.add('pointer-events-none');
                }
            });
        }
    }

    // Xử lý khi modal đóng
    function handleModalClose() {
        console.log('[Modal-Handler] Processing modal close');
        const modalElements = document.querySelectorAll('.modal.show');

        // Nếu không còn modal nào mở
        if (modalElements.length === 0) {
            // Xóa class khỏi body
            document.body.classList.remove('modal-open');
            
            // Khôi phục trạng thái chatbot
            const chatbot = document.querySelector('.chatbot-container');
            const chatbotPopup = document.querySelector('.chatbot-popup');
            
            if (chatbot) {
                chatbot.style.zIndex = '1500';
                
                // Chỉ bật lại sự kiện chuột nếu chatbot đang mở
                if (chatbotPopup && chatbotPopup.classList.contains('active')) {
                    chatbot.classList.remove('pointer-events-none');
                } else {
                    chatbot.classList.add('pointer-events-none');
                }
            }
        }
    }
    
    console.log('[Modal-Handler] Modal interaction handler initialized');
});
