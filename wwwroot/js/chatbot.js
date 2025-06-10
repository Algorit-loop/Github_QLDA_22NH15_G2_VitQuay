/**
 * Advanced AI Chatbot JS with Gemini API Integration
 * Core functionality for E-Learning Chatbot
 */

// Variables
let isChatbotInitialized = false;
let messages = [];
const MAX_MESSAGES = 200; // Tăng số lượng tin nhắn tối đa có thể lưu trữ

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    console.log('[AI Chatbot] Initializing components');
    
    // Create chat UI if it doesn't exist
    createChatbotUI();
    
    // Add event listeners
    setupEventListeners();
    
    // Load previous messages from storage
    loadMessages();
    
    // Update welcome message with user name
    updateWelcomeMessage();
    
    // Add keyboard shortcuts
    setupKeyboardShortcuts();
    
    // Theo dõi các sự kiện đăng nhập/đăng xuất
    monitorAuthEvents();
});

// Create chatbot UI elements
function createChatbotUI() {
    // Check if chatbot already exists
    if (document.querySelector('.chatbot-container')) {
        return;
    }
    
    // Create main container
    const chatbotContainer = document.createElement('div');
    chatbotContainer.className = 'chatbot-container';
      // Create HTML structure
    chatbotContainer.innerHTML = `
        <div class="chatbot-popup" role="dialog" aria-labelledby="chatbot-title" aria-hidden="true">
            <div class="chatbot-header">                <h3 id="chatbot-title"><i class="fas fa-robot mr-2"></i> Trợ lý AI</h3>
                <div class="chatbot-header-actions">
                    <button id="clear-chatbot" title="Xóa lịch sử (Alt+X)" aria-label="Xóa toàn bộ lịch sử trò chuyện"><i class="fas fa-trash-alt"></i></button>
                    <button id="expand-chatbot" title="Phóng to/Thu nhỏ (Alt+E)" aria-label="Phóng to hoặc thu nhỏ cửa sổ trò chuyện"><i class="fas fa-expand-alt"></i></button>
                    <button id="close-chatbot" title="Đóng (Esc)" aria-label="Đóng cửa sổ trò chuyện"><i class="fas fa-times"></i></button>
                </div>
            </div>            <div class="chatbot-body" id="chatbot-messages" role="log" aria-live="polite">
                <div class="welcome-message">
                    Xin chào! Tôi là trợ lý AI của E-Learning VitQuay.<br>
                    Tôi có thể giúp gì cho bạn?<br>
                    <small class="keyboard-hint">
                        Phím tắt: Alt+C (mở/đóng), Alt+E (phóng to), Alt+X (xóa lịch sử), Esc (đóng)
                    </small>
                </div>
            </div>
            <div class="chatbot-footer">
                <div class="chatbot-input-container">
                    <input type="text" id="chatbot-input" class="chatbot-input" placeholder="Hỏi điều gì đó..." 
                           aria-label="Nhập câu hỏi của bạn" />
                    <button id="chatbot-send" class="chatbot-send-btn" aria-label="Gửi câu hỏi">
                        <i class="fas fa-paper-plane"></i>
                    </button>
                </div>
            </div>
        </div>
        <div class="chatbot-launcher" id="chatbot-launcher" title="Mở trợ lý AI (Alt+C)" 
             role="button" aria-label="Mở trợ lý AI" tabindex="0" aria-expanded="false">
            <div class="chatbot-launcher-icon open"><i class="fas fa-comments"></i></div>
            <div class="chatbot-launcher-icon close"><i class="fas fa-times"></i></div>
        </div>
    `;
    
    // Add to document
    document.body.appendChild(chatbotContainer);
}

// Set up event listeners
function setupEventListeners() {
    // Launcher button
    const chatbotLauncher = document.getElementById('chatbot-launcher');
    if (chatbotLauncher) {
        chatbotLauncher.addEventListener('click', function(e) {
            e.preventDefault();
            toggleChatbot();
            return false;
        });
    }
      // Close and expand buttons
    const closeButton = document.getElementById('close-chatbot');
    if (closeButton) {
        closeButton.addEventListener('click', function() {
            toggleChatbot(false);
        });
    }
    
    const expandButton = document.getElementById('expand-chatbot');
    if (expandButton) {
        expandButton.addEventListener('click', function() {
            toggleExpandChatbot();
        });
    }
    
    // Clear history button
    const clearButton = document.getElementById('clear-chatbot');
    if (clearButton) {
        clearButton.addEventListener('click', function() {
            clearChatHistory();
        });
    }
    
    // Send message button
    const sendButton = document.getElementById('chatbot-send');
    if (sendButton) {
        sendButton.addEventListener('click', sendUserMessage);
    }
    
    // Input field (submit on Enter)
    const inputField = document.getElementById('chatbot-input');
    if (inputField) {
        inputField.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                sendUserMessage();
            }
        });
    }
}

// Toggle chatbot visibility
function toggleChatbot(show) {
    const chatbotPopup = document.querySelector('.chatbot-popup');
    const launcher = document.getElementById('chatbot-launcher');
    const expandButton = document.getElementById('expand-chatbot');
    const chatbotContainer = document.querySelector('.chatbot-container');
    
    if (show === undefined) {
        show = !chatbotPopup.classList.contains('active');
    }
    
    if (show) {
        chatbotPopup.classList.add('active');
        launcher.classList.add('active');
        
        // Khi mở chatbot, cho phép tương tác với nó (trừ khi có modal đang hiển thị)
        const openModals = document.querySelectorAll('.modal.show');
        if (openModals.length === 0 && chatbotContainer) {
            chatbotContainer.classList.remove('pointer-events-none');
        }
        
        // Update ARIA attributes
        chatbotPopup.setAttribute('aria-hidden', 'false');
        launcher.setAttribute('aria-expanded', 'true');
        
        // Check if a modal is open
        if (openModals.length === 0) {
            // No modals open, ensure chatbot has correct z-index
            if (chatbotContainer) {
                chatbotContainer.style.zIndex = '1500'; // Normal z-index
            }
        }
          // Focus on input
        setTimeout(() => {
            const input = document.getElementById('chatbot-input');
            if (input) input.focus();
        }, 300);
    } else {
        chatbotPopup.classList.remove('active');
        launcher.classList.remove('active');
        
        // Khi đóng chatbot, vô hiệu hóa sự kiện chuột để tránh che phủ các phần tử khác
        if (chatbotContainer) {
            chatbotContainer.classList.add('pointer-events-none');
        }
        
        // Update ARIA attributes
        chatbotPopup.setAttribute('aria-hidden', 'true');
        launcher.setAttribute('aria-expanded', 'false');
        
        // Khi đóng chatbot, đảm bảo rằng nó trở về kích thước bình thường
        if (chatbotPopup.classList.contains('expanded')) {
            chatbotPopup.classList.remove('expanded');
            if (expandButton) {
                expandButton.innerHTML = '<i class="fas fa-expand-alt"></i>';
            }
        }
    }
}

// Toggle expand/collapse chatbot function
function toggleExpandChatbot() {
    const chatbotPopup = document.querySelector('.chatbot-popup');
    const expandButton = document.getElementById('expand-chatbot');
    
    if (chatbotPopup && expandButton) {
        if (chatbotPopup.classList.contains('expanded')) {
            // Thu nhỏ chatbot
            chatbotPopup.classList.remove('expanded');
            expandButton.innerHTML = '<i class="fas fa-expand-alt"></i>';
            expandButton.setAttribute('aria-label', 'Phóng to cửa sổ trò chuyện');
            expandButton.setAttribute('title', 'Phóng to (Alt+E)');
        } else {
            // Phóng to chatbot
            chatbotPopup.classList.add('expanded');
            expandButton.innerHTML = '<i class="fas fa-compress-alt"></i>';
            expandButton.setAttribute('aria-label', 'Thu nhỏ cửa sổ trò chuyện');
            expandButton.setAttribute('title', 'Thu nhỏ (Alt+E)');
        }
        
        // Scroll to bottom after expanding/collapsing
        const messagesContainer = document.getElementById('chatbot-messages');
        if (messagesContainer) {
            setTimeout(() => {
                scrollToBottom();
            }, 300);
        }
    }
}

// Send message function
function sendUserMessage() {
    const inputField = document.getElementById('chatbot-input');
    const messagesContainer = document.getElementById('chatbot-messages');
    
    if (!inputField || !messagesContainer) return;
    
    const userMessage = inputField.value.trim();
    if (!userMessage) return;
    
    // Clear input
    inputField.value = '';
    
    // Add user message to UI
    appendMessage('user', userMessage);
    
    // Show thinking indicator
    const thinkingId = showThinkingIndicator();
    
    // Store message
    messages.push({role: 'user', content: userMessage});
    saveMessages();
    
    // Call API to get response
    fetchAiResponse(userMessage)
        .then(response => {
            // Remove thinking indicator
            hideThinkingIndicator(thinkingId);
            
            // Add AI response to UI
            appendMessage('bot', response);
            
            // Store response
            messages.push({role: 'bot', content: response});
            saveMessages();
            
            // Scroll to bottom
            scrollToBottom();
        })
        .catch(error => {
            console.error('Error fetching AI response:', error);
            
            // Remove thinking indicator
            hideThinkingIndicator(thinkingId);
            
            // Show error message
            appendMessage('bot', 'Xin lỗi, tôi đang gặp sự cố kết nối. Vui lòng thử lại sau.');
            scrollToBottom();
        });
}

// Fetch AI response from server
async function fetchAiResponse(message) {
    try {
        // Chuẩn bị lịch sử tin nhắn để gửi lên server
        // Giới hạn số lượng tin nhắn gửi đi để tránh quá tải
        const historyLimit = 15; // Chỉ gửi 15 tin nhắn gần nhất
        const chatHistory = messages.slice(-historyLimit);
        
        // Use the correct route to match our controller action
        const response = await fetch('/Chatbot/SendMessage', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            body: JSON.stringify({ 
                Message: message,
                ChatHistory: chatHistory // Gửi lịch sử trò chuyện
            })
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        return data.message || 'Xin lỗi, tôi không hiểu câu hỏi của bạn.';
    } catch (error) {
        console.error('Error in fetchAiResponse:', error);
        throw error;
    }
}

// Append message to chat
function appendMessage(sender, content) {
    const messagesContainer = document.getElementById('chatbot-messages');
    if (!messagesContainer) return;
    
    const messageDiv = document.createElement('div');
    messageDiv.className = `chatbot-message ${sender}`;
    
    // Add appropriate role for screen readers
    if (sender === 'user') {
        messageDiv.setAttribute('role', 'complementary');
        messageDiv.setAttribute('aria-label', 'Tin nhắn của bạn');
    } else if (sender === 'bot') {
        messageDiv.setAttribute('role', 'region');
        messageDiv.setAttribute('aria-label', 'Trả lời từ trợ lý AI');
    }
    
    // Create message content
    messageDiv.innerHTML = formatMessageContent(content);
    
    // Add avatar for user or bot
    const avatarDiv = document.createElement('div');
    if (sender === 'user') {
        avatarDiv.className = 'user-avatar';
        avatarDiv.innerHTML = '<i class="fas fa-user"></i>';
        avatarDiv.setAttribute('aria-hidden', 'true');
    } else if (sender === 'bot') {
        avatarDiv.className = 'bot-avatar';
        avatarDiv.innerHTML = '<i class="fas fa-robot"></i>';
        avatarDiv.setAttribute('aria-hidden', 'true');
    }
    
    messageDiv.appendChild(avatarDiv);
    messagesContainer.appendChild(messageDiv);
    
    // Announce for screen readers that a new message has arrived
    if (sender === 'bot') {
        const ariaLiveRegion = document.createElement('div');
        ariaLiveRegion.className = 'sr-only';
        ariaLiveRegion.setAttribute('aria-live', 'assertive');
        ariaLiveRegion.textContent = 'Trợ lý AI đã trả lời: ' + content.replace(/<[^>]*>?/gm, '');
        document.body.appendChild(ariaLiveRegion);
        
        // Remove after announcement
        setTimeout(() => {
            document.body.removeChild(ariaLiveRegion);
        }, 1000);
    }
    
    // Scroll to bottom
    scrollToBottom();
}

// Format message content with markdown-like syntax
function formatMessageContent(content) {
    if (!content) return '';
    
    // Convert URLs to links
    content = content.replace(
        /(https?:\/\/[^\s]+)/g, 
        '<a href="$1" target="_blank" rel="noopener noreferrer">$1</a>'
    );
    
    // Convert **bold** text
    content = content.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
    
    // Convert *italic* text
    content = content.replace(/\*(.*?)\*/g, '<em>$1</em>');
    
    // Convert line breaks
    content = content.replace(/\n/g, '<br>');
    
    return content;
}

// Show thinking indicator
function showThinkingIndicator() {
    const messagesContainer = document.getElementById('chatbot-messages');
    if (!messagesContainer) return null;
    
    const id = 'thinking-' + Date.now();
    
    const thinkingDiv = document.createElement('div');
    thinkingDiv.className = 'chatbot-message thinking';
    thinkingDiv.id = id;
    thinkingDiv.innerHTML = `
        <div class="thinking-dot"></div>
        <div class="thinking-dot"></div>
        <div class="thinking-dot"></div>
    `;
    
    messagesContainer.appendChild(thinkingDiv);
    scrollToBottom();
    
    return id;
}

// Hide thinking indicator
function hideThinkingIndicator(id) {
    if (!id) return;
    
    const thinkingDiv = document.getElementById(id);
    if (thinkingDiv) {
        thinkingDiv.remove();
    }
}

// Scroll chat to bottom
function scrollToBottom() {
    const messagesContainer = document.getElementById('chatbot-messages');
    if (messagesContainer) {
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }
}

// Load messages from localStorage for current user
function loadMessages() {
    try {
        // Lấy userId từ userData (được thiết lập trong _Layout.cshtml)
        const storageKey = getUserStorageKey();
        console.log(`[AI Chatbot] Loading messages for key: ${storageKey}`);
        
        const savedMessages = localStorage.getItem(storageKey);
        if (savedMessages) {
            messages = JSON.parse(savedMessages).slice(0, MAX_MESSAGES);
            
            // Display last 5 messages
            const lastMessages = messages.slice(-5);
            lastMessages.forEach(msg => {
                appendMessage(msg.role, msg.content);
            });
            console.log(`[AI Chatbot] Loaded ${lastMessages.length} messages for user`);
        } else {
            console.log('[AI Chatbot] No saved messages found for this user');
            messages = []; // Reset messages for new user
        }
    } catch (error) {
        console.error('Error loading messages:', error);
        messages = []; // Reset on error
    }
}

// Save messages to localStorage for current user
function saveMessages() {
    try {
        // Keep only the last MAX_MESSAGES messages
        if (messages.length > MAX_MESSAGES) {
            messages = messages.slice(-MAX_MESSAGES);
        }
        
        // Sử dụng userId để lưu riêng tin nhắn cho từng người dùng
        const storageKey = getUserStorageKey();
        localStorage.setItem(storageKey, JSON.stringify(messages));
        console.log(`[AI Chatbot] Saved ${messages.length} messages for key: ${storageKey}`);
    } catch (error) {
        console.error('Error saving messages:', error);
    }
}

// Tạo key lưu trữ dựa vào thông tin người dùng
function getUserStorageKey() {
    // Kiểm tra xem userData đã được thiết lập chưa
    if (window.userData && window.userData.isAuthenticated) {
        // Nếu người dùng đã đăng nhập, sử dụng userId làm key
        return `aiChatbotMessages_${window.userData.userId}`;
    } else {
        // Nếu chưa đăng nhập, sử dụng key mặc định là "guest"
        return 'aiChatbotMessages_guest';
    }
}

// Theo dõi sự kiện đăng nhập/đăng xuất
function monitorAuthEvents() {
    // Lưu trữ userId hiện tại
    let currentUserId = window.userData && window.userData.isAuthenticated ? window.userData.userId : 'guest';
    console.log(`[AI Chatbot] Current user: ${currentUserId}`);
    
    // Kiểm tra thay đổi đăng nhập/đăng xuất mỗi 2 giây
    setInterval(function() {
        const newUserId = window.userData && window.userData.isAuthenticated ? window.userData.userId : 'guest';
        
        // Nếu người dùng thay đổi
        if (newUserId !== currentUserId) {
            console.log(`[AI Chatbot] User changed: ${currentUserId} -> ${newUserId}`);
            currentUserId = newUserId;
            
            // Xóa tin nhắn hiển thị hiện tại
            const messagesContainer = document.getElementById('chatbot-messages');
            if (messagesContainer) {
                // Giữ lại chỉ tin nhắn chào mừng
                const welcomeMessage = messagesContainer.querySelector('.welcome-message');
                messagesContainer.innerHTML = '';
                if (welcomeMessage) {
                    messagesContainer.appendChild(welcomeMessage);
                }
            }
            
            // Tải lại lịch sử tin nhắn cho người dùng mới
            loadMessages();
            
            // Cập nhật lời chào
            updateWelcomeMessage();
        }
    }, 2000);
}

// Cập nhật lời chào dựa vào người dùng đăng nhập
function updateWelcomeMessage() {
    const welcomeMessage = document.querySelector('.welcome-message');
    if (welcomeMessage && window.userData) {
        const userName = window.userData.isAuthenticated ? window.userData.userName : 'bạn';
        let welcomeHTML = `Xin chào ${userName}! Tôi là trợ lý AI của E-Learning VitQuay.<br>
                  Tôi có thể giúp gì cho bạn?<br>
                  <small class="keyboard-hint">
                      Phím tắt: Alt+C (mở/đóng), Alt+E (phóng to), Alt+X (xóa lịch sử), Esc (đóng)
                  </small>`;
        welcomeMessage.innerHTML = welcomeHTML;
    }
}

// Xóa lịch sử trò chuyện của người dùng hiện tại
function clearChatHistory() {
    // Hiển thị hộp thoại xác nhận
    if (confirm('Bạn có chắc chắn muốn xóa toàn bộ lịch sử trò chuyện không?')) {
        // Xóa từ bộ nhớ
        messages = [];
        const storageKey = getUserStorageKey();
        localStorage.removeItem(storageKey);
        console.log(`[AI Chatbot] Cleared chat history for key: ${storageKey}`);
        
        // Xóa khỏi giao diện
        const messagesContainer = document.getElementById('chatbot-messages');
        if (messagesContainer) {
            // Giữ lại chỉ tin nhắn chào mừng
            const welcomeMessage = document.querySelector('.welcome-message');
            messagesContainer.innerHTML = '';
            if (welcomeMessage) {
                messagesContainer.appendChild(welcomeMessage.cloneNode(true));
            } else {
                // Nếu không tìm thấy tin nhắn chào mừng, tạo mới
                const newWelcome = document.createElement('div');
                newWelcome.className = 'welcome-message';
                newWelcome.innerHTML = `
                    Xin chào! Tôi là trợ lý AI của E-Learning.<br>
                    Tôi có thể giúp gì cho bạn?<br>
                    <small class="keyboard-hint">
                        Phím tắt: Alt+C (mở/đóng), Alt+E (phóng to), Alt+X (xóa lịch sử), Esc (đóng)
                    </small>
                `;
                messagesContainer.appendChild(newWelcome);
            }
        }
        
        // Hiển thị thông báo thành công
        showNotification('Đã xóa toàn bộ lịch sử trò chuyện!');
    }
}

// Hiển thị thông báo
function showNotification(message) {
    const notificationDiv = document.createElement('div');
    notificationDiv.className = 'chatbot-notification';
    notificationDiv.textContent = message;
    
    document.body.appendChild(notificationDiv);
    
    // Hiệu ứng hiển thị
    setTimeout(() => {
        notificationDiv.classList.add('show');
    }, 10);
    
    // Tự động ẩn sau 3 giây
    setTimeout(() => {
        notificationDiv.classList.remove('show');
        setTimeout(() => {
            document.body.removeChild(notificationDiv);
        }, 300);
    }, 3000);
}

// Setup keyboard shortcuts for chatbot
function setupKeyboardShortcuts() {
    document.addEventListener('keydown', function(e) {
        // Alt + C to toggle (open/close) chatbot
        if (e.altKey && e.key === 'c') {
            e.preventDefault();
            toggleChatbot();
        }
        
        // Alt + E to toggle expand/collapse when chatbot is open
        if (e.altKey && e.key === 'e') {
            e.preventDefault();
            const chatbotPopup = document.querySelector('.chatbot-popup');
            if (chatbotPopup && chatbotPopup.classList.contains('active')) {
                toggleExpandChatbot();
            }
        }
        
        // Alt + X to clear chat history when chatbot is open
        if (e.altKey && e.key === 'x') {
            e.preventDefault();
            const chatbotPopup = document.querySelector('.chatbot-popup');
            if (chatbotPopup && chatbotPopup.classList.contains('active')) {
                clearChatHistory();
            }
        }
        
        // Escape key to close or collapse expanded chatbot
        if (e.key === 'Escape') {
            const chatbotPopup = document.querySelector('.chatbot-popup');
            if (chatbotPopup && chatbotPopup.classList.contains('active')) {
                if (chatbotPopup.classList.contains('expanded')) {
                    // If expanded, first collapse it
                    toggleExpandChatbot();
                } else {
                    // If not expanded, close it
                    toggleChatbot(false);
                }
                e.preventDefault();
            }
        }
    });
}

// Theo dõi sự kiện đăng nhập/đăng xuất
function monitorAuthEvents() {
    // Lưu trữ userId hiện tại
    let currentUserId = window.userData && window.userData.isAuthenticated ? window.userData.userId : 'guest';
    console.log(`[AI Chatbot] Current user: ${currentUserId}`);
    
    // Kiểm tra thay đổi đăng nhập/đăng xuất mỗi 2 giây
    setInterval(function() {
        const newUserId = window.userData && window.userData.isAuthenticated ? window.userData.userId : 'guest';
        
        // Nếu người dùng thay đổi
        if (newUserId !== currentUserId) {
            console.log(`[AI Chatbot] User changed: ${currentUserId} -> ${newUserId}`);
            currentUserId = newUserId;
            
            // Xóa tin nhắn hiển thị hiện tại
            const messagesContainer = document.getElementById('chatbot-messages');
            if (messagesContainer) {
                // Giữ lại chỉ tin nhắn chào mừng
                const welcomeMessage = messagesContainer.querySelector('.welcome-message');
                messagesContainer.innerHTML = '';
                if (welcomeMessage) {
                    messagesContainer.appendChild(welcomeMessage);
                }
            }
            
            // Tải lại lịch sử tin nhắn cho người dùng mới
            loadMessages();
            
            // Cập nhật lời chào
            updateWelcomeMessage();
        }
    }, 2000);
    
    // Theo dõi modal để sửa các vấn đề về z-index
    monitorModalEvents();
}

// Theo dõi sự kiện mở/đóng modal để xử lý tương tác chatbot
function monitorModalEvents() {
    // Theo dõi tất cả các nút mở modal
    document.querySelectorAll('[data-bs-toggle="modal"]').forEach(button => {
        button.addEventListener('click', () => {
            // Tạm thời vô hiệu hóa sự kiện chuột của chatbot khi modal mở
            setTimeout(() => {
                const chatbotContainer = document.querySelector('.chatbot-container');
                if (chatbotContainer) {
                    chatbotContainer.classList.add('pointer-events-none');
                    chatbotContainer.style.zIndex = '1000'; // Giảm z-index khi có modal
                    console.log('[AI Chatbot] Disabling chatbot pointer events due to modal open');
                }
                
                // Đảm bảo modal có z-index cao
                document.querySelectorAll('.modal.show').forEach(modal => {
                    modal.style.zIndex = '2050';
                });
                
                document.querySelectorAll('.modal.show .modal-dialog').forEach(dialog => {
                    dialog.style.zIndex = '2060';
                });
                
                document.querySelectorAll('.modal.show .modal-content').forEach(content => {
                    content.style.zIndex = '2070';
                });
                
                // Tăng z-index cho các phần tử tương tác trong modal
                document.querySelectorAll('.modal.show .btn, .modal.show button, .modal.show input, .modal.show a, .modal.show select, .modal.show .form-control, .modal.show .form-select').forEach(element => {
                    element.style.position = 'relative';
                    element.style.zIndex = '2100'; // Cao hơn cả modal
                    element.style.pointerEvents = 'auto'; // Đảm bảo nhận sự kiện chuột
                });
            }, 100);
        });
    });
    
    // Theo dõi đóng modal để khôi phục chatbot
    document.addEventListener('hidden.bs.modal', () => {
        setTimeout(() => {
            const modals = document.querySelectorAll('.modal.show');
            if (modals.length === 0) {
                // Không còn modal nào mở, khôi phục chatbot
                const chatbotContainer = document.querySelector('.chatbot-container');
                const chatbotPopup = document.querySelector('.chatbot-popup');
                
                if (chatbotContainer) {
                    chatbotContainer.style.zIndex = '1500'; // Khôi phục z-index ban đầu
                    // Chỉ bật sự kiện chuột nếu chatbot đang mở
                    if (chatbotPopup && chatbotPopup.classList.contains('active')) {
                        chatbotContainer.classList.remove('pointer-events-none');
                    } else {
                        chatbotContainer.classList.add('pointer-events-none');
                    }
                    console.log('[AI Chatbot] Restoring chatbot state after modal close');
                }
            }
        }, 100);
    });
}

// Cập nhật lời chào dựa vào người dùng đăng nhập
function updateWelcomeMessage() {
    const welcomeMessage = document.querySelector('.welcome-message');
    if (welcomeMessage && window.userData) {
        const userName = window.userData.isAuthenticated ? window.userData.userName : 'bạn';
        let welcomeHTML = `Xin chào ${userName}! Tôi là trợ lý AI của E-Learning VitQuay.<br>
                  Tôi có thể giúp gì cho bạn?<br>
                  <small class="keyboard-hint">
                      Phím tắt: Alt+C (mở/đóng), Alt+E (phóng to), Alt+X (xóa lịch sử), Esc (đóng)
                  </small>`;
        welcomeMessage.innerHTML = welcomeHTML;
    }
}
